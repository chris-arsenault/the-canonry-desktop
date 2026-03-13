using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.NameForge;
using TheCanonry.NameForge.Types;

namespace TheCanonry.Desktop.NameForge;

// ============================================================================
// Display item types
// ============================================================================

internal sealed class CultureListItem : ViewModelBase
{
    private bool _isConfigured;
    private int _domainCount;
    private int _lexemeListCount;
    private int _grammarCount;
    private int _profileCount;

    public required string Id { get; init; }
    public required string Name { get; init; }

    public bool IsConfigured
    {
        get => _isConfigured;
        set => SetProperty(ref _isConfigured, value);
    }

    public int DomainCount
    {
        get => _domainCount;
        set => SetProperty(ref _domainCount, value);
    }

    public int LexemeListCount
    {
        get => _lexemeListCount;
        set => SetProperty(ref _lexemeListCount, value);
    }

    public int GrammarCount
    {
        get => _grammarCount;
        set => SetProperty(ref _grammarCount, value);
    }

    public int ProfileCount
    {
        get => _profileCount;
        set => SetProperty(ref _profileCount, value);
    }
}

internal sealed class GeneratedNameItem
{
    public required string Name { get; init; }
    public required string Strategy { get; init; }
    public required string Detail { get; init; }
}

internal sealed class CoverageCell
{
    public required string EntityKindId { get; init; }
    public required string CultureId { get; init; }
    public required string ProfileId { get; init; }
    public required bool IsCovered { get; init; }
    public required string Label { get; init; }
}

internal sealed class CoverageRow
{
    public required string EntityKindId { get; init; }
    public required string EntityKindName { get; init; }
    public required ObservableCollection<CoverageCell> Cells { get; init; }
}

internal sealed class CoverageColumn
{
    public required string Header { get; init; }
}

// ============================================================================
// ViewModel
// ============================================================================

internal sealed class NameForgeViewModel : ViewModelBase, ISectionedViewModel
{
    private static readonly JsonSerializerOptions NamingJsonOptions = CreateNamingJsonOptions();

    private readonly ProjectService _project;

    // Section switching
    private string _activeSection = "workshop";

    // Culture list
    private CultureListItem? _selectedCulture;

    // Workshop: loaded culture data
    private Culture? _loadedCulture;
    private string _workshopTab = "domains";
    private string _statusMessage = "";

    // Workshop > Domains
    private NamingDomain? _selectedDomain;
    private string _editDomainId = "";
    private string _editDomainAppliesKinds = "";
    private string _editDomainAppliesSubKinds = "";
    private string _editDomainAppliesTags = "";
    private string _editConsonants = "";
    private string _editVowels = "";
    private string _editSyllableTemplates = "";
    private string _editForbiddenClusters = "";
    private string _editFavoredClusters = "";
    private string _editLengthMin = "2";
    private string _editLengthMax = "4";
    private string _editPrefixes = "";
    private string _editSuffixes = "";
    private string _editInfixes = "";
    private string _editWordRoots = "";
    private string _editHonorifics = "";
    private string _editStructure = "";
    private double _editApostropheRate;
    private double _editHyphenRate;
    private string _editStyleCapitalization = "Title";
    private string _editPreferredEndings = "";
    private string _editRhythmBias = "Neutral";

    // Workshop > Lexeme Lists
    private LexemeList? _selectedLexemeList;
    private string _editLexemeId = "";
    private string _editLexemeDescription = "";
    private string _editLexemeSource = "";
    private string _editLexemeEntries = "";

    // Workshop > Grammars
    private Grammar? _selectedGrammar;
    private string _editGrammarId = "";
    private string _editGrammarDescription = "";
    private string _editGrammarStart = "";
    private string _editGrammarCapitalization = "Title";
    private string _grammarRulesDisplay = "";

    // Workshop > Profiles
    private Profile? _selectedProfile;
    private string _editProfileId = "";
    private string _editProfileName = "";
    private string _editProfileEntityKinds = "";
    private bool _editProfileIsDefault;
    private string _profileStrategyDisplay = "";

    // Generate
    private string _generateCultureId = "";
    private string _generateProfileId = "";
    private string _generateEntityKind = "";
    private string _generateSubtype = "";
    private string _generateProminence = "";
    private int _generateCount = 10;
    private string _generateSeed = "";
    private string _strategyUsageDisplay = "";

    public NameForgeViewModel(ProjectService project)
    {
        _project = project;

        Cultures = new ObservableCollection<CultureListItem>();
        Domains = new ObservableCollection<NamingDomain>();
        LexemeLists = new ObservableCollection<LexemeList>();
        Grammars = new ObservableCollection<Grammar>();
        Profiles = new ObservableCollection<Profile>();
        GeneratedNames = new ObservableCollection<GeneratedNameItem>();
        CoverageRows = new ObservableCollection<CoverageRow>();
        CoverageColumns = new ObservableCollection<CoverageColumn>();
        AvailableProfiles = new ObservableCollection<Profile>();
        AvailableSubtypes = new ObservableCollection<string>();

        SwitchToWorkshopCommand = new RelayCommand(() => ActiveSection = "workshop");
        SwitchToGenerateCommand = new RelayCommand(() => ActiveSection = "generate");
        SwitchToCoverageCommand = new RelayCommand(() => ActiveSection = "coverage");

        SwitchToDomainsTabCommand = new RelayCommand(() => WorkshopTab = "domains");
        SwitchToLexemesTabCommand = new RelayCommand(() => WorkshopTab = "lexemes");
        SwitchToGrammarsTabCommand = new RelayCommand(() => WorkshopTab = "grammars");
        SwitchToProfilesTabCommand = new RelayCommand(() => WorkshopTab = "profiles");

        SaveDomainCommand = new RelayCommand(SaveDomain, () => _loadedCulture is not null);
        AddDomainCommand = new RelayCommand(AddDomain, () => _loadedCulture is not null);
        RemoveDomainCommand = new RelayCommand(RemoveDomain, () => _selectedDomain is not null);

        SaveLexemeListCommand = new RelayCommand(SaveLexemeList, () => _loadedCulture is not null);
        AddLexemeListCommand = new RelayCommand(AddLexemeList, () => _loadedCulture is not null);
        RemoveLexemeListCommand = new RelayCommand(RemoveLexemeList, () => _selectedLexemeList is not null);

        SaveProfileCommand = new RelayCommand(SaveProfile, () => _loadedCulture is not null);
        AddProfileCommand = new RelayCommand(AddProfile, () => _loadedCulture is not null);
        RemoveProfileCommand = new RelayCommand(RemoveProfile, () => _selectedProfile is not null);

        GenerateCommand = new RelayCommand(RunGenerate, () => !string.IsNullOrEmpty(_generateCultureId));

        _project.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProjectService.IsLoaded))
                RefreshCultureList();
        };

        RefreshCultureList();
    }

    // ========================================================================
    // Section switching
    // ========================================================================

    public string ActiveSection
    {
        get => _activeSection;
        set
        {
            if (SetProperty(ref _activeSection, value))
            {
                OnPropertyChanged(nameof(IsWorkshopActive));
                OnPropertyChanged(nameof(IsGenerateActive));
                OnPropertyChanged(nameof(IsCoverageActive));

                if (value == "coverage")
                    BuildCoverageMatrix();
            }
        }
    }

    public bool IsWorkshopActive => _activeSection == "workshop";
    public bool IsGenerateActive => _activeSection == "generate";
    public bool IsCoverageActive => _activeSection == "coverage";

    public string WorkshopTab
    {
        get => _workshopTab;
        set
        {
            if (SetProperty(ref _workshopTab, value))
            {
                OnPropertyChanged(nameof(IsDomainsTab));
                OnPropertyChanged(nameof(IsLexemesTab));
                OnPropertyChanged(nameof(IsGrammarsTab));
                OnPropertyChanged(nameof(IsProfilesTab));
            }
        }
    }

    public bool IsDomainsTab => _workshopTab == "domains";
    public bool IsLexemesTab => _workshopTab == "lexemes";
    public bool IsGrammarsTab => _workshopTab == "grammars";
    public bool IsProfilesTab => _workshopTab == "profiles";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    // ========================================================================
    // Commands
    // ========================================================================

    public ICommand SwitchToWorkshopCommand { get; }
    public ICommand SwitchToGenerateCommand { get; }
    public ICommand SwitchToCoverageCommand { get; }

    public ICommand SwitchToDomainsTabCommand { get; }
    public ICommand SwitchToLexemesTabCommand { get; }
    public ICommand SwitchToGrammarsTabCommand { get; }
    public ICommand SwitchToProfilesTabCommand { get; }

    public ICommand SaveDomainCommand { get; }
    public ICommand AddDomainCommand { get; }
    public ICommand RemoveDomainCommand { get; }

    public ICommand SaveLexemeListCommand { get; }
    public ICommand AddLexemeListCommand { get; }
    public ICommand RemoveLexemeListCommand { get; }

    public ICommand SaveProfileCommand { get; }
    public ICommand AddProfileCommand { get; }
    public ICommand RemoveProfileCommand { get; }

    public ICommand GenerateCommand { get; }

    // ========================================================================
    // Collections
    // ========================================================================

    public ObservableCollection<CultureListItem> Cultures { get; }
    public ObservableCollection<NamingDomain> Domains { get; }
    public ObservableCollection<LexemeList> LexemeLists { get; }
    public ObservableCollection<Grammar> Grammars { get; }
    public ObservableCollection<Profile> Profiles { get; }
    public ObservableCollection<GeneratedNameItem> GeneratedNames { get; }
    public ObservableCollection<CoverageRow> CoverageRows { get; }
    public ObservableCollection<CoverageColumn> CoverageColumns { get; }
    public ObservableCollection<Profile> AvailableProfiles { get; }
    public ObservableCollection<string> AvailableSubtypes { get; }

    // ========================================================================
    // Culture selection
    // ========================================================================

    public CultureListItem? SelectedCulture
    {
        get => _selectedCulture;
        set
        {
            if (SetProperty(ref _selectedCulture, value))
                LoadSelectedCulture();
        }
    }

    // ========================================================================
    // Workshop > Domains
    // ========================================================================

    public NamingDomain? SelectedDomain
    {
        get => _selectedDomain;
        set
        {
            if (SetProperty(ref _selectedDomain, value))
            {
                PopulateDomainEditor(value);
                ((RelayCommand)RemoveDomainCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string EditDomainId { get => _editDomainId; set => SetProperty(ref _editDomainId, value); }
    public string EditDomainAppliesKinds { get => _editDomainAppliesKinds; set => SetProperty(ref _editDomainAppliesKinds, value); }
    public string EditDomainAppliesSubKinds { get => _editDomainAppliesSubKinds; set => SetProperty(ref _editDomainAppliesSubKinds, value); }
    public string EditDomainAppliesTags { get => _editDomainAppliesTags; set => SetProperty(ref _editDomainAppliesTags, value); }
    public string EditConsonants { get => _editConsonants; set => SetProperty(ref _editConsonants, value); }
    public string EditVowels { get => _editVowels; set => SetProperty(ref _editVowels, value); }
    public string EditSyllableTemplates { get => _editSyllableTemplates; set => SetProperty(ref _editSyllableTemplates, value); }
    public string EditForbiddenClusters { get => _editForbiddenClusters; set => SetProperty(ref _editForbiddenClusters, value); }
    public string EditFavoredClusters { get => _editFavoredClusters; set => SetProperty(ref _editFavoredClusters, value); }
    public string EditLengthMin { get => _editLengthMin; set => SetProperty(ref _editLengthMin, value); }
    public string EditLengthMax { get => _editLengthMax; set => SetProperty(ref _editLengthMax, value); }
    public string EditPrefixes { get => _editPrefixes; set => SetProperty(ref _editPrefixes, value); }
    public string EditSuffixes { get => _editSuffixes; set => SetProperty(ref _editSuffixes, value); }
    public string EditInfixes { get => _editInfixes; set => SetProperty(ref _editInfixes, value); }
    public string EditWordRoots { get => _editWordRoots; set => SetProperty(ref _editWordRoots, value); }
    public string EditHonorifics { get => _editHonorifics; set => SetProperty(ref _editHonorifics, value); }
    public string EditStructure { get => _editStructure; set => SetProperty(ref _editStructure, value); }
    public double EditApostropheRate { get => _editApostropheRate; set => SetProperty(ref _editApostropheRate, value); }
    public double EditHyphenRate { get => _editHyphenRate; set => SetProperty(ref _editHyphenRate, value); }
    public string EditStyleCapitalization { get => _editStyleCapitalization; set => SetProperty(ref _editStyleCapitalization, value); }
    public string EditPreferredEndings { get => _editPreferredEndings; set => SetProperty(ref _editPreferredEndings, value); }
    public string EditRhythmBias { get => _editRhythmBias; set => SetProperty(ref _editRhythmBias, value); }

    // ========================================================================
    // Workshop > Lexeme Lists
    // ========================================================================

    public LexemeList? SelectedLexemeList
    {
        get => _selectedLexemeList;
        set
        {
            if (SetProperty(ref _selectedLexemeList, value))
            {
                PopulateLexemeEditor(value);
                ((RelayCommand)RemoveLexemeListCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string EditLexemeId { get => _editLexemeId; set => SetProperty(ref _editLexemeId, value); }
    public string EditLexemeDescription { get => _editLexemeDescription; set => SetProperty(ref _editLexemeDescription, value); }
    public string EditLexemeSource { get => _editLexemeSource; set => SetProperty(ref _editLexemeSource, value); }
    public string EditLexemeEntries { get => _editLexemeEntries; set => SetProperty(ref _editLexemeEntries, value); }

    // ========================================================================
    // Workshop > Grammars
    // ========================================================================

    public Grammar? SelectedGrammar
    {
        get => _selectedGrammar;
        set
        {
            if (SetProperty(ref _selectedGrammar, value))
                PopulateGrammarEditor(value);
        }
    }

    public string EditGrammarId { get => _editGrammarId; set => SetProperty(ref _editGrammarId, value); }
    public string EditGrammarDescription { get => _editGrammarDescription; set => SetProperty(ref _editGrammarDescription, value); }
    public string EditGrammarStart { get => _editGrammarStart; set => SetProperty(ref _editGrammarStart, value); }
    public string EditGrammarCapitalization { get => _editGrammarCapitalization; set => SetProperty(ref _editGrammarCapitalization, value); }
    public string GrammarRulesDisplay { get => _grammarRulesDisplay; set => SetProperty(ref _grammarRulesDisplay, value); }

    // ========================================================================
    // Workshop > Profiles
    // ========================================================================

    public Profile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                PopulateProfileEditor(value);
                ((RelayCommand)RemoveProfileCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string EditProfileId { get => _editProfileId; set => SetProperty(ref _editProfileId, value); }
    public string EditProfileName { get => _editProfileName; set => SetProperty(ref _editProfileName, value); }
    public string EditProfileEntityKinds { get => _editProfileEntityKinds; set => SetProperty(ref _editProfileEntityKinds, value); }

    public bool EditProfileIsDefault
    {
        get => _editProfileIsDefault;
        set => SetProperty(ref _editProfileIsDefault, value);
    }

    public string ProfileStrategyDisplay { get => _profileStrategyDisplay; set => SetProperty(ref _profileStrategyDisplay, value); }

    // ========================================================================
    // Generate section
    // ========================================================================

    public string GenerateCultureId
    {
        get => _generateCultureId;
        set
        {
            if (SetProperty(ref _generateCultureId, value))
            {
                ((RelayCommand)GenerateCommand).RaiseCanExecuteChanged();
                RefreshAvailableProfiles();
            }
        }
    }

    public string GenerateProfileId
    {
        get => _generateProfileId;
        set => SetProperty(ref _generateProfileId, value);
    }

    public string GenerateEntityKind
    {
        get => _generateEntityKind;
        set
        {
            if (SetProperty(ref _generateEntityKind, value))
                RefreshAvailableSubtypes();
        }
    }

    public string GenerateSubtype { get => _generateSubtype; set => SetProperty(ref _generateSubtype, value); }
    public string GenerateProminence { get => _generateProminence; set => SetProperty(ref _generateProminence, value); }

    public int GenerateCount
    {
        get => _generateCount;
        set => SetProperty(ref _generateCount, Math.Clamp(value, 1, 100));
    }

    public string GenerateSeed { get => _generateSeed; set => SetProperty(ref _generateSeed, value); }
    public string StrategyUsageDisplay { get => _strategyUsageDisplay; set => SetProperty(ref _strategyUsageDisplay, value); }

    // ========================================================================
    // Culture list management
    // ========================================================================

    private void RefreshCultureList()
    {
        Cultures.Clear();
        if (!_project.IsLoaded) return;

        foreach (var culture in _project.Cultures)
        {
            var item = new CultureListItem
            {
                Id = culture.Id.Value,
                Name = culture.Name,
            };

            var namingPath = GetNamingFilePath(culture.Id.Value);
            if (File.Exists(namingPath))
            {
                try
                {
                    var json = File.ReadAllText(namingPath);
                    var data = JsonSerializer.Deserialize<Culture>(json, NamingJsonOptions);
                    if (data is not null)
                    {
                        item.IsConfigured = true;
                        item.DomainCount = data.Domains.Length;
                        item.LexemeListCount = data.LexemeLists.Count;
                        item.GrammarCount = data.Grammars.Length;
                        item.ProfileCount = data.Profiles.Length;
                    }
                }
                catch
                {
                    // Malformed file — show as unconfigured
                }
            }

            Cultures.Add(item);
        }
    }

    private void LoadSelectedCulture()
    {
        _loadedCulture = null;
        Domains.Clear();
        LexemeLists.Clear();
        Grammars.Clear();
        Profiles.Clear();
        ClearDomainEditor();
        ClearLexemeEditor();
        ClearGrammarEditor();
        ClearProfileEditor();

        RaiseWorkshopCanExecute();

        if (_selectedCulture is null) return;

        var path = GetNamingFilePath(_selectedCulture.Id);
        if (!File.Exists(path))
        {
            StatusMessage = $"No naming data for {_selectedCulture.Name} (file not found)";
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            _loadedCulture = JsonSerializer.Deserialize<Culture>(json, NamingJsonOptions);

            if (_loadedCulture is null)
            {
                StatusMessage = $"Failed to parse naming data for {_selectedCulture.Name}";
                return;
            }

            foreach (var d in _loadedCulture.Domains)
                Domains.Add(d);
            foreach (var l in _loadedCulture.LexemeLists.Values)
                LexemeLists.Add(l);
            foreach (var g in _loadedCulture.Grammars)
                Grammars.Add(g);
            foreach (var p in _loadedCulture.Profiles)
                Profiles.Add(p);

            StatusMessage = $"Loaded {_selectedCulture.Name}: {Domains.Count} domains, " +
                            $"{LexemeLists.Count} lexeme lists, {Grammars.Count} grammars, {Profiles.Count} profiles";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            StatusMessage = $"Error loading naming data: {ex.Message}";
        }

        RaiseWorkshopCanExecute();
    }

    private void RaiseWorkshopCanExecute()
    {
        ((RelayCommand)SaveDomainCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddDomainCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveDomainCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveLexemeListCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddLexemeListCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveLexemeListCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveProfileCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AddProfileCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveProfileCommand).RaiseCanExecuteChanged();
    }

    // ========================================================================
    // Workshop > Domains editing
    // ========================================================================

    private void PopulateDomainEditor(NamingDomain? domain)
    {
        if (domain is null)
        {
            ClearDomainEditor();
            return;
        }

        EditDomainId = domain.Id;
        EditDomainAppliesKinds = string.Join(", ", domain.AppliesTo.Kind);
        EditDomainAppliesSubKinds = string.Join(", ", domain.AppliesTo.SubKind);
        EditDomainAppliesTags = string.Join(", ", domain.AppliesTo.Tags);
        EditConsonants = string.Join(", ", domain.Phonology.Consonants);
        EditVowels = string.Join(", ", domain.Phonology.Vowels);
        EditSyllableTemplates = string.Join(", ", domain.Phonology.SyllableTemplates);
        EditForbiddenClusters = string.Join(", ", domain.Phonology.ForbiddenClusters);
        EditFavoredClusters = string.Join(", ", domain.Phonology.FavoredClusters);
        EditLengthMin = domain.Phonology.LengthRange.Length > 0 ? domain.Phonology.LengthRange[0].ToString() : "2";
        EditLengthMax = domain.Phonology.LengthRange.Length > 1 ? domain.Phonology.LengthRange[1].ToString() : "4";
        EditPrefixes = string.Join(", ", domain.Morphology.Prefixes);
        EditSuffixes = string.Join(", ", domain.Morphology.Suffixes);
        EditInfixes = string.Join(", ", domain.Morphology.Infixes);
        EditWordRoots = string.Join(", ", domain.Morphology.WordRoots);
        EditHonorifics = string.Join(", ", domain.Morphology.Honorifics);
        EditStructure = string.Join(", ", domain.Morphology.Structure);
        EditApostropheRate = domain.Style.ApostropheRate;
        EditHyphenRate = domain.Style.HyphenRate;
        EditStyleCapitalization = domain.Style.Capitalization.ToString();
        EditPreferredEndings = string.Join(", ", domain.Style.PreferredEndings);
        EditRhythmBias = domain.Style.RhythmBias.ToString();
    }

    private void ClearDomainEditor()
    {
        EditDomainId = "";
        EditDomainAppliesKinds = "";
        EditDomainAppliesSubKinds = "";
        EditDomainAppliesTags = "";
        EditConsonants = "";
        EditVowels = "";
        EditSyllableTemplates = "";
        EditForbiddenClusters = "";
        EditFavoredClusters = "";
        EditLengthMin = "2";
        EditLengthMax = "4";
        EditPrefixes = "";
        EditSuffixes = "";
        EditInfixes = "";
        EditWordRoots = "";
        EditHonorifics = "";
        EditStructure = "";
        EditApostropheRate = 0;
        EditHyphenRate = 0;
        EditStyleCapitalization = "Title";
        EditPreferredEndings = "";
        EditRhythmBias = "Neutral";
    }

    private NamingDomain BuildDomainFromEditor()
    {
        return new NamingDomain
        {
            Id = EditDomainId.Trim(),
            AppliesTo = new AppliesTo
            {
                Kind = SplitComma(EditDomainAppliesKinds),
                SubKind = SplitComma(EditDomainAppliesSubKinds),
                Tags = SplitComma(EditDomainAppliesTags),
            },
            Phonology = new PhonologyProfile
            {
                Consonants = SplitComma(EditConsonants),
                Vowels = SplitComma(EditVowels),
                SyllableTemplates = SplitComma(EditSyllableTemplates),
                ForbiddenClusters = SplitComma(EditForbiddenClusters),
                FavoredClusters = SplitComma(EditFavoredClusters),
                LengthRange = [ParseInt(EditLengthMin, 2), ParseInt(EditLengthMax, 4)],
            },
            Morphology = new MorphologyProfile
            {
                Prefixes = SplitComma(EditPrefixes),
                Suffixes = SplitComma(EditSuffixes),
                Infixes = SplitComma(EditInfixes),
                WordRoots = SplitComma(EditWordRoots),
                Honorifics = SplitComma(EditHonorifics),
                Structure = SplitComma(EditStructure),
            },
            Style = new StyleRules
            {
                ApostropheRate = EditApostropheRate,
                HyphenRate = EditHyphenRate,
                Capitalization = ParseEnum<Capitalization>(EditStyleCapitalization),
                PreferredEndings = SplitComma(EditPreferredEndings),
                RhythmBias = ParseEnum<RhythmBias>(EditRhythmBias),
            },
        };
    }

    private void SaveDomain()
    {
        if (_loadedCulture is null || string.IsNullOrWhiteSpace(EditDomainId)) return;

        var edited = BuildDomainFromEditor();
        var domains = _loadedCulture.Domains.ToList();

        var existingIdx = domains.FindIndex(d => d.Id == _selectedDomain?.Id);
        if (existingIdx >= 0)
            domains[existingIdx] = edited;
        else
            domains.Add(edited);

        SaveCultureWithUpdate(c => c with { Domains = [.. domains] });
        StatusMessage = $"Saved domain '{edited.Id}'";
    }

    private void AddDomain()
    {
        if (_loadedCulture is null) return;

        var newId = $"domain-{_loadedCulture.Domains.Length + 1}";
        var newDomain = new NamingDomain
        {
            Id = newId,
            AppliesTo = new AppliesTo { Kind = [] },
            Phonology = new PhonologyProfile
            {
                Consonants = ["t", "n", "s", "r", "l"],
                Vowels = ["a", "e", "i", "o"],
                SyllableTemplates = ["CV", "CVC"],
                LengthRange = [2, 3],
            },
            Morphology = new MorphologyProfile
            {
                Structure = ["root"],
            },
            Style = new StyleRules(),
        };

        var domains = _loadedCulture.Domains.ToList();
        domains.Add(newDomain);
        SaveCultureWithUpdate(c => c with { Domains = [.. domains] });
        SelectedDomain = Domains.FirstOrDefault(d => d.Id == newId);
        StatusMessage = $"Added new domain '{newId}'";
    }

    private void RemoveDomain()
    {
        if (_loadedCulture is null || _selectedDomain is null) return;

        var removeId = _selectedDomain.Id;
        var domains = _loadedCulture.Domains.Where(d => d.Id != removeId).ToArray();
        SaveCultureWithUpdate(c => c with { Domains = domains });
        StatusMessage = $"Removed domain '{removeId}'";
    }

    // ========================================================================
    // Workshop > Lexeme Lists editing
    // ========================================================================

    private void PopulateLexemeEditor(LexemeList? list)
    {
        if (list is null)
        {
            ClearLexemeEditor();
            return;
        }

        EditLexemeId = list.Id;
        EditLexemeDescription = list.Description;
        EditLexemeSource = list.Source;
        EditLexemeEntries = string.Join("\n", list.Entries);
    }

    private void ClearLexemeEditor()
    {
        EditLexemeId = "";
        EditLexemeDescription = "";
        EditLexemeSource = "";
        EditLexemeEntries = "";
    }

    private void SaveLexemeList()
    {
        if (_loadedCulture is null || string.IsNullOrWhiteSpace(EditLexemeId)) return;

        var edited = new LexemeList
        {
            Id = EditLexemeId.Trim(),
            Description = EditLexemeDescription.Trim(),
            Source = EditLexemeSource.Trim(),
            Entries = EditLexemeEntries
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => e.Length > 0)
                .ToArray(),
        };

        var lists = new Dictionary<string, LexemeList>(_loadedCulture.LexemeLists);

        // If renaming, remove old key
        if (_selectedLexemeList is not null && _selectedLexemeList.Id != edited.Id)
            lists.Remove(_selectedLexemeList.Id);

        lists[edited.Id] = edited;

        SaveCultureWithUpdate(c => c with { LexemeLists = lists });
        SelectedLexemeList = LexemeLists.FirstOrDefault(l => l.Id == edited.Id);
        StatusMessage = $"Saved lexeme list '{edited.Id}' ({edited.Entries.Length} entries)";
    }

    private void AddLexemeList()
    {
        if (_loadedCulture is null) return;

        var newId = $"list-{_loadedCulture.LexemeLists.Count + 1}";
        var newList = new LexemeList
        {
            Id = newId,
            Entries = [],
        };

        var lists = new Dictionary<string, LexemeList>(_loadedCulture.LexemeLists)
        {
            [newId] = newList,
        };

        SaveCultureWithUpdate(c => c with { LexemeLists = lists });
        SelectedLexemeList = LexemeLists.FirstOrDefault(l => l.Id == newId);
        StatusMessage = $"Added new lexeme list '{newId}'";
    }

    private void RemoveLexemeList()
    {
        if (_loadedCulture is null || _selectedLexemeList is null) return;

        var removeId = _selectedLexemeList.Id;
        var lists = new Dictionary<string, LexemeList>(_loadedCulture.LexemeLists);
        lists.Remove(removeId);
        SaveCultureWithUpdate(c => c with { LexemeLists = lists });
        StatusMessage = $"Removed lexeme list '{removeId}'";
    }

    // ========================================================================
    // Workshop > Grammars (read-only rules, editable metadata)
    // ========================================================================

    private void PopulateGrammarEditor(Grammar? grammar)
    {
        if (grammar is null)
        {
            ClearGrammarEditor();
            return;
        }

        EditGrammarId = grammar.Id;
        EditGrammarDescription = grammar.Description;
        EditGrammarStart = grammar.Start;
        EditGrammarCapitalization = grammar.Capitalization.ToString();

        // Format rules for display
        var lines = new List<string>();
        foreach (var (symbol, productions) in grammar.Rules)
        {
            var prodStrings = productions.Select(p => string.Join(" ", p));
            lines.Add($"{symbol} -> {string.Join(" | ", prodStrings)}");
        }
        GrammarRulesDisplay = string.Join("\n", lines);
    }

    private void ClearGrammarEditor()
    {
        EditGrammarId = "";
        EditGrammarDescription = "";
        EditGrammarStart = "";
        EditGrammarCapitalization = "Title";
        GrammarRulesDisplay = "";
    }

    // ========================================================================
    // Workshop > Profiles editing
    // ========================================================================

    private void PopulateProfileEditor(Profile? profile)
    {
        if (profile is null)
        {
            ClearProfileEditor();
            return;
        }

        EditProfileId = profile.Id;
        EditProfileName = profile.Name;
        EditProfileEntityKinds = string.Join(", ", profile.EntityKinds);
        EditProfileIsDefault = profile.IsDefault;

        // Build strategy display
        var lines = new List<string>();
        foreach (var group in profile.StrategyGroups)
        {
            var label = string.IsNullOrEmpty(group.Name) ? "(unnamed)" : group.Name;
            lines.Add($"Group: {label}  (priority {group.Priority})");

            if (group.Conditions is not null)
            {
                if (group.Conditions.EntityKinds.Length > 0)
                    lines.Add($"  Kinds: {string.Join(", ", group.Conditions.EntityKinds)}");
                if (group.Conditions.Subtypes.Length > 0)
                    lines.Add($"  Subtypes: {string.Join(", ", group.Conditions.Subtypes)}");
                if (group.Conditions.Prominence.Length > 0)
                    lines.Add($"  Prominence: {string.Join(", ", group.Conditions.Prominence)}");
                if (group.Conditions.Tags.Length > 0)
                    lines.Add($"  Tags: {string.Join(", ", group.Conditions.Tags)} (matchAll={group.Conditions.TagMatchAll})");
            }

            foreach (var strategy in group.Strategies)
            {
                var detail = strategy.Type switch
                {
                    StrategyType.Grammar => $"grammar:{strategy.GrammarId}",
                    StrategyType.Phonotactic => $"phonotactic:{strategy.DomainId}",
                    _ => strategy.Type.ToString(),
                };
                lines.Add($"  [{strategy.Weight:F1}] {detail}");
            }

            lines.Add("");
        }
        ProfileStrategyDisplay = string.Join("\n", lines);
    }

    private void ClearProfileEditor()
    {
        EditProfileId = "";
        EditProfileName = "";
        EditProfileEntityKinds = "";
        EditProfileIsDefault = false;
        ProfileStrategyDisplay = "";
    }

    private void SaveProfile()
    {
        if (_loadedCulture is null || string.IsNullOrWhiteSpace(EditProfileId)) return;

        // Preserve existing strategy groups — only update metadata fields
        var existingProfile = _selectedProfile;
        var strategyGroups = existingProfile?.StrategyGroups ?? [];

        var edited = new Profile
        {
            Id = EditProfileId.Trim(),
            Name = EditProfileName.Trim(),
            EntityKinds = SplitComma(EditProfileEntityKinds),
            IsDefault = EditProfileIsDefault,
            StrategyGroups = strategyGroups,
        };

        var profiles = _loadedCulture.Profiles.ToList();
        var existingIdx = profiles.FindIndex(p => p.Id == existingProfile?.Id);
        if (existingIdx >= 0)
            profiles[existingIdx] = edited;
        else
            profiles.Add(edited);

        SaveCultureWithUpdate(c => c with { Profiles = [.. profiles] });
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == edited.Id);
        StatusMessage = $"Saved profile '{edited.Id}'";
    }

    private void AddProfile()
    {
        if (_loadedCulture is null) return;

        var newId = $"profile-{_loadedCulture.Profiles.Length + 1}";
        var newProfile = new Profile
        {
            Id = newId,
            Name = newId,
            StrategyGroups =
            [
                new StrategyGroup
                {
                    Name = "default",
                    Strategies =
                    [
                        new Strategy
                        {
                            Type = StrategyType.Phonotactic,
                            Weight = 1.0,
                            DomainId = _loadedCulture.Domains.Length > 0
                                ? _loadedCulture.Domains[0].Id
                                : "",
                        },
                    ],
                },
            ],
        };

        var profiles = _loadedCulture.Profiles.ToList();
        profiles.Add(newProfile);
        SaveCultureWithUpdate(c => c with { Profiles = [.. profiles] });
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == newId);
        StatusMessage = $"Added new profile '{newId}'";
    }

    private void RemoveProfile()
    {
        if (_loadedCulture is null || _selectedProfile is null) return;

        var removeId = _selectedProfile.Id;
        var profiles = _loadedCulture.Profiles.Where(p => p.Id != removeId).ToArray();
        SaveCultureWithUpdate(c => c with { Profiles = profiles });
        StatusMessage = $"Removed profile '{removeId}'";
    }

    // ========================================================================
    // Culture save helper
    // ========================================================================

    private void SaveCultureWithUpdate(Func<Culture, Culture> mutator)
    {
        if (_loadedCulture is null || _selectedCulture is null) return;

        var updated = mutator(_loadedCulture);
        var path = GetNamingFilePath(_selectedCulture.Id);

        var dir = Path.GetDirectoryName(path);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(updated, NamingJsonOptions);
        File.WriteAllText(path, json);

        // Reload
        _selectedCulture.DomainCount = updated.Domains.Length;
        _selectedCulture.LexemeListCount = updated.LexemeLists.Count;
        _selectedCulture.GrammarCount = updated.Grammars.Length;
        _selectedCulture.ProfileCount = updated.Profiles.Length;
        _selectedCulture.IsConfigured = true;

        _loadedCulture = updated;

        Domains.Clear();
        foreach (var d in updated.Domains) Domains.Add(d);
        LexemeLists.Clear();
        foreach (var l in updated.LexemeLists.Values) LexemeLists.Add(l);
        Grammars.Clear();
        foreach (var g in updated.Grammars) Grammars.Add(g);
        Profiles.Clear();
        foreach (var p in updated.Profiles) Profiles.Add(p);
    }

    private string GetNamingFilePath(string cultureId)
    {
        return Path.Combine(_project.ConfigPath, "naming", $"{cultureId}.json");
    }

    // ========================================================================
    // Generate section
    // ========================================================================

    private void RefreshAvailableProfiles()
    {
        AvailableProfiles.Clear();
        GenerateProfileId = "";

        if (string.IsNullOrEmpty(GenerateCultureId)) return;

        var culture = LoadCultureForGeneration(GenerateCultureId);
        if (culture is null) return;

        foreach (var profile in culture.Profiles)
            AvailableProfiles.Add(profile);
    }

    private void RefreshAvailableSubtypes()
    {
        AvailableSubtypes.Clear();
        GenerateSubtype = "";

        if (string.IsNullOrEmpty(GenerateEntityKind)) return;

        var ekDef = _project.EntityKinds.FirstOrDefault(ek => ek.Kind.Value == GenerateEntityKind);
        if (ekDef is null) return;

        foreach (var sub in ekDef.Subtypes)
            AvailableSubtypes.Add(sub.Id);
    }

    private void RunGenerate()
    {
        GeneratedNames.Clear();
        StrategyUsageDisplay = "";

        if (string.IsNullOrEmpty(GenerateCultureId))
        {
            StatusMessage = "Select a culture to generate names";
            return;
        }

        var culture = LoadCultureForGeneration(GenerateCultureId);
        if (culture is null)
        {
            StatusMessage = $"No naming data found for culture '{GenerateCultureId}'";
            return;
        }

        try
        {
            var request = new GenerateRequest
            {
                CultureId = GenerateCultureId,
                ProfileId = GenerateProfileId,
                Kind = GenerateEntityKind,
                Subtype = GenerateSubtype,
                Prominence = GenerateProminence.ToLowerInvariant(),
                Count = GenerateCount,
                Seed = string.IsNullOrWhiteSpace(GenerateSeed) ? "" : GenerateSeed,
            };

            var result = NameGenerator.Generate(culture, request);

            for (var i = 0; i < result.Names.Length; i++)
            {
                var debug = i < result.DebugInfo.Length ? result.DebugInfo[i] : null;
                GeneratedNames.Add(new GeneratedNameItem
                {
                    Name = result.Names[i],
                    Strategy = debug?.StrategyType ?? "",
                    Detail = debug?.StrategyUsed ?? "",
                });
            }

            // Build strategy usage summary
            var usageParts = result.StrategyUsage
                .Where(kv => kv.Value > 0)
                .Select(kv => $"{kv.Key}: {kv.Value}")
                .ToArray();
            StrategyUsageDisplay = usageParts.Length > 0
                ? string.Join("  |  ", usageParts)
                : "No strategies used";

            StatusMessage = $"Generated {result.Names.Length} names for {culture.Name}";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            StatusMessage = $"Generation error: {ex.Message}";
        }
    }

    private Culture? LoadCultureForGeneration(string cultureId)
    {
        // If already loaded in workshop, use that
        if (_loadedCulture is not null && _loadedCulture.Id == cultureId)
            return _loadedCulture;

        var path = GetNamingFilePath(cultureId);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Culture>(json, NamingJsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // ========================================================================
    // Coverage section
    // ========================================================================

    private void BuildCoverageMatrix()
    {
        CoverageRows.Clear();
        CoverageColumns.Clear();

        if (!_project.IsLoaded) return;

        // Collect all profiles across all cultures
        var cultureProfiles = new List<(string CultureId, string CultureName, Culture Culture)>();
        foreach (var cultureDef in _project.Cultures)
        {
            var culture = LoadCultureForGeneration(cultureDef.Id.Value);
            if (culture is not null)
                cultureProfiles.Add((cultureDef.Id.Value, cultureDef.Name, culture));
        }

        // Build columns: one per (culture, profile)
        var columns = new List<(string CultureId, string CultureName, Profile Profile)>();
        foreach (var (cId, cName, culture) in cultureProfiles)
        {
            foreach (var profile in culture.Profiles)
                columns.Add((cId, cName, profile));
        }

        foreach (var (_, cName, profile) in columns)
        {
            var header = $"{cName}\n{profile.Id}";
            if (profile.IsDefault)
                header += " *";
            CoverageColumns.Add(new CoverageColumn { Header = header });
        }

        // Build rows: one per entity kind
        foreach (var ekDef in _project.EntityKinds)
        {
            var cells = new ObservableCollection<CoverageCell>();
            foreach (var (cId, _, profile) in columns)
            {
                var isCovered = profile.EntityKinds.Length == 0 && profile.IsDefault
                    || profile.EntityKinds.Contains(ekDef.Kind.Value);

                cells.Add(new CoverageCell
                {
                    EntityKindId = ekDef.Kind.Value,
                    CultureId = cId,
                    ProfileId = profile.Id,
                    IsCovered = isCovered,
                    Label = isCovered ? "Y" : "-",
                });
            }

            CoverageRows.Add(new CoverageRow
            {
                EntityKindId = ekDef.Kind.Value,
                EntityKindName = ekDef.Kind.Value,
                Cells = cells,
            });
        }
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static string[] SplitComma(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => s.Length > 0)
                    .ToArray();
    }

    private static int ParseInt(string input, int defaultValue)
    {
        return int.TryParse(input.Trim(), out var v) ? v : defaultValue;
    }

    private static T ParseEnum<T>(string input) where T : struct, Enum
    {
        return Enum.TryParse<T>(input.Trim(), ignoreCase: true, out var v) ? v : default;
    }

    private static JsonSerializerOptions CreateNamingJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
