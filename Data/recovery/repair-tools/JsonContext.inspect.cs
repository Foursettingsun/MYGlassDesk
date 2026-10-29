using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GlassDesk.Core.Input;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;

namespace GlassDesk.Core.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
[JsonSerializable(typeof(GlassDeskSettings))]
[JsonSerializable(typeof(AppLibraryData))]
[JsonSerializable(typeof(DockLayout))]
[JsonSerializable(typeof(LauncherLayout))]
[JsonSerializable(typeof(BackupManifest))]
[GeneratedCode("System.Text.Json.SourceGeneration", "10.0.14.42308")]
public sealed class GlassDeskJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<bool?>? _NullableBoolean;

	private JsonTypeInfo<double>? _Double;

	private JsonTypeInfo<InputAction>? _InputAction;

	private JsonTypeInfo<AppEntry>? _AppEntry;

	private JsonTypeInfo<AppItemKind>? _AppItemKind;

	private JsonTypeInfo<AppLibraryData>? _AppLibraryData;

	private JsonTypeInfo<BackupManifest>? _BackupManifest;

	private JsonTypeInfo<DockLayout>? _DockLayout;

	private JsonTypeInfo<GlassMaterialMode>? _GlassMaterialMode;

	private JsonTypeInfo<LauncherGroup>? _LauncherGroup;

	private JsonTypeInfo<LauncherLayout>? _LauncherLayout;

	private JsonTypeInfo<LauncherPage>? _LauncherPage;

	private JsonTypeInfo<LauncherSlot>? _LauncherSlot;

	private JsonTypeInfo<LauncherSlotKind>? _LauncherSlotKind;

	private JsonTypeInfo<AppearanceSettings>? _AppearanceSettings;

	private JsonTypeInfo<DesktopTaskbarSettings>? _DesktopTaskbarSettings;

	private JsonTypeInfo<DisplaySettings>? _DisplaySettings;

	private JsonTypeInfo<DockPlacement>? _DockPlacement;

	private JsonTypeInfo<DockSettings>? _DockSettings;

	private JsonTypeInfo<GlassDeskSettings>? _GlassDeskSettings;

	private JsonTypeInfo<HotCorner>? _HotCorner;

	private JsonTypeInfo<HotkeyDefinition>? _HotkeyDefinition;

	private JsonTypeInfo<LauncherSettings>? _LauncherSettings;

	private JsonTypeInfo<PerformanceSettings>? _PerformanceSettings;

	private JsonTypeInfo<ShortcutSettings>? _ShortcutSettings;

	private JsonTypeInfo<StatusBarEdge>? _StatusBarEdge;

	private JsonTypeInfo<ThemeMode>? _ThemeMode;

	private JsonTypeInfo<List<AppEntry>>? _ListAppEntry;

	private JsonTypeInfo<List<LauncherGroup>>? _ListLauncherGroup;

	private JsonTypeInfo<List<LauncherPage>>? _ListLauncherPage;

	private JsonTypeInfo<List<LauncherSlot>>? _ListLauncherSlot;

	private JsonTypeInfo<List<string>>? _ListString;

	private JsonTypeInfo<DateTimeOffset>? _DateTimeOffset;

	private JsonTypeInfo<DateTimeOffset?>? _NullableDateTimeOffset;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = true
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_id = JsonEncodedText.Encode("id");

	private static readonly JsonEncodedText PropName_displayName = JsonEncodedText.Encode("displayName");

	private static readonly JsonEncodedText PropName_kind = JsonEncodedText.Encode("kind");

	private static readonly JsonEncodedText PropName_target = JsonEncodedText.Encode("target");

	private static readonly JsonEncodedText PropName_arguments = JsonEncodedText.Encode("arguments");

	private static readonly JsonEncodedText PropName_workingDirectory = JsonEncodedText.Encode("workingDirectory");

	private static readonly JsonEncodedText PropName_appUserModelId = JsonEncodedText.Encode("appUserModelId");

	private static readonly JsonEncodedText PropName_iconSource = JsonEncodedText.Encode("iconSource");

	private static readonly JsonEncodedText PropName_customIconPath = JsonEncodedText.Encode("customIconPath");

	private static readonly JsonEncodedText PropName_searchAliases = JsonEncodedText.Encode("searchAliases");

	private static readonly JsonEncodedText PropName_pinyinIndex = JsonEncodedText.Encode("pinyinIndex");

	private static readonly JsonEncodedText PropName_pinyinInitials = JsonEncodedText.Encode("pinyinInitials");

	private static readonly JsonEncodedText PropName_isTargetMissing = JsonEncodedText.Encode("isTargetMissing");

	private static readonly JsonEncodedText PropName_addedAtUtc = JsonEncodedText.Encode("addedAtUtc");

	private static readonly JsonEncodedText PropName_hasLaunchTarget = JsonEncodedText.Encode("hasLaunchTarget");

	private static readonly JsonEncodedText PropName_isUsable = JsonEncodedText.Encode("isUsable");

	private static readonly JsonEncodedText PropName_schemaVersion = JsonEncodedText.Encode("schemaVersion");

	private static readonly JsonEncodedText PropName_items = JsonEncodedText.Encode("items");

	private static readonly JsonEncodedText PropName_product = JsonEncodedText.Encode("product");

	private static readonly JsonEncodedText PropName_productVersion = JsonEncodedText.Encode("productVersion");

	private static readonly JsonEncodedText PropName_createdAtUtc = JsonEncodedText.Encode("createdAtUtc");

	private static readonly JsonEncodedText PropName_files = JsonEncodedText.Encode("files");

	private static readonly JsonEncodedText PropName_pinnedItemIds = JsonEncodedText.Encode("pinnedItemIds");

	private static readonly JsonEncodedText PropName_name = JsonEncodedText.Encode("name");

	private static readonly JsonEncodedText PropName_itemIds = JsonEncodedText.Encode("itemIds");

	private static readonly JsonEncodedText PropName_pages = JsonEncodedText.Encode("pages");

	private static readonly JsonEncodedText PropName_flatOrder = JsonEncodedText.Encode("flatOrder");

	private static readonly JsonEncodedText PropName_filterGroups = JsonEncodedText.Encode("filterGroups");

	private static readonly JsonEncodedText PropName_excludedAppIds = JsonEncodedText.Encode("excludedAppIds");

	private static readonly JsonEncodedText PropName_slots = JsonEncodedText.Encode("slots");

	private static readonly JsonEncodedText PropName_itemId = JsonEncodedText.Encode("itemId");

	private static readonly JsonEncodedText PropName_group = JsonEncodedText.Encode("group");

	private static readonly JsonEncodedText PropName_pinBarsToDesktop = JsonEncodedText.Encode("pinBarsToDesktop");

	private static readonly JsonEncodedText PropName_dockGlassTransparency = JsonEncodedText.Encode("dockGlassTransparency");

	private static readonly JsonEncodedText PropName_launcherGlassTransparency = JsonEncodedText.Encode("launcherGlassTransparency");

	private static readonly JsonEncodedText PropName_dockBuiltinIconBrightness = JsonEncodedText.Encode("dockBuiltinIconBrightness");

	private static readonly JsonEncodedText PropName_launchpadIconFilled = JsonEncodedText.Encode("launchpadIconFilled");

	private static readonly JsonEncodedText PropName_launcherTextBrightness = JsonEncodedText.Encode("launcherTextBrightness");

	private static readonly JsonEncodedText PropName_statusBarTextBrightness = JsonEncodedText.Encode("statusBarTextBrightness");

	private static readonly JsonEncodedText PropName_glassBackgroundClarity = JsonEncodedText.Encode("glassBackgroundClarity");

	private static readonly JsonEncodedText PropName_glassTintOpacity = JsonEncodedText.Encode("glassTintOpacity");

	private static readonly JsonEncodedText PropName_glassMaterial = JsonEncodedText.Encode("glassMaterial");

	private static readonly JsonEncodedText PropName_theme = JsonEncodedText.Encode("theme");

	private static readonly JsonEncodedText PropName_accentColor = JsonEncodedText.Encode("accentColor");

	private static readonly JsonEncodedText PropName_useSystemBackdrop = JsonEncodedText.Encode("useSystemBackdrop");

	private static readonly JsonEncodedText PropName_reduceMotion = JsonEncodedText.Encode("reduceMotion");

	private static readonly JsonEncodedText PropName_liveBackdrop = JsonEncodedText.Encode("liveBackdrop");

	private static readonly JsonEncodedText PropName_reduceTransparency = JsonEncodedText.Encode("reduceTransparency");

	private static readonly JsonEncodedText PropName_dockCornerRadius = JsonEncodedText.Encode("dockCornerRadius");

	private static readonly JsonEncodedText PropName_launcherCornerRadius = JsonEncodedText.Encode("launcherCornerRadius");

	private static readonly JsonEncodedText PropName_toggleDesktopIcons = JsonEncodedText.Encode("toggleDesktopIcons");

	private static readonly JsonEncodedText PropName_blockTaskbarEdgeReveal = JsonEncodedText.Encode("blockTaskbarEdgeReveal");

	private static readonly JsonEncodedText PropName_taskbarAutoHideEnabled = JsonEncodedText.Encode("taskbarAutoHideEnabled");

	private static readonly JsonEncodedText PropName_dockDisplayId = JsonEncodedText.Encode("dockDisplayId");

	private static readonly JsonEncodedText PropName_launcherFollowsPointer = JsonEncodedText.Encode("launcherFollowsPointer");

	private static readonly JsonEncodedText PropName_suppressTriggersWhenFullscreen = JsonEncodedText.Encode("suppressTriggersWhenFullscreen");

	private static readonly JsonEncodedText PropName_sizeScale = JsonEncodedText.Encode("sizeScale");

	private static readonly JsonEncodedText PropName_showLauncherEntry = JsonEncodedText.Encode("showLauncherEntry");

	private static readonly JsonEncodedText PropName_showThisPcEntry = JsonEncodedText.Encode("showThisPcEntry");

	private static readonly JsonEncodedText PropName_showDesktopIconsEntry = JsonEncodedText.Encode("showDesktopIconsEntry");

	private static readonly JsonEncodedText PropName_launcherCustomIconPath = JsonEncodedText.Encode("launcherCustomIconPath");

	private static readonly JsonEncodedText PropName_thisPcCustomIconPath = JsonEncodedText.Encode("thisPcCustomIconPath");

	private static readonly JsonEncodedText PropName_desktopIconsCustomIconPath = JsonEncodedText.Encode("desktopIconsCustomIconPath");

	private static readonly JsonEncodedText PropName_showStatusBar = JsonEncodedText.Encode("showStatusBar");

	private static readonly JsonEncodedText PropName_showStatusBarNetworkSpeed = JsonEncodedText.Encode("showStatusBarNetworkSpeed");

	private static readonly JsonEncodedText PropName_statusBarSizeScale = JsonEncodedText.Encode("statusBarSizeScale");

	private static readonly JsonEncodedText PropName_statusBarEdge = JsonEncodedText.Encode("statusBarEdge");

	private static readonly JsonEncodedText PropName_statusBarOffsetX = JsonEncodedText.Encode("statusBarOffsetX");

	private static readonly JsonEncodedText PropName_statusBarOffsetY = JsonEncodedText.Encode("statusBarOffsetY");

	private static readonly JsonEncodedText PropName_frameLengthDip = JsonEncodedText.Encode("frameLengthDip");

	private static readonly JsonEncodedText PropName_iconSize = JsonEncodedText.Encode("iconSize");

	private static readonly JsonEncodedText PropName_iconSpacing = JsonEncodedText.Encode("iconSpacing");

	private static readonly JsonEncodedText PropName_magnificationPeak = JsonEncodedText.Encode("magnificationPeak");

	private static readonly JsonEncodedText PropName_placement = JsonEncodedText.Encode("placement");

	private static readonly JsonEncodedText PropName_offsetX = JsonEncodedText.Encode("offsetX");

	private static readonly JsonEncodedText PropName_offsetY = JsonEncodedText.Encode("offsetY");

	private static readonly JsonEncodedText PropName_autoHide = JsonEncodedText.Encode("autoHide");

	private static readonly JsonEncodedText PropName_autoHideLeaveDelayMs = JsonEncodedText.Encode("autoHideLeaveDelayMs");

	private static readonly JsonEncodedText PropName_edgeRevealEnabled = JsonEncodedText.Encode("edgeRevealEnabled");

	private static readonly JsonEncodedText PropName_edgeHotzoneSize = JsonEncodedText.Encode("edgeHotzoneSize");

	private static readonly JsonEncodedText PropName_blockRevealWhenCovered = JsonEncodedText.Encode("blockRevealWhenCovered");

	private static readonly JsonEncodedText PropName_targetDisplayId = JsonEncodedText.Encode("targetDisplayId");

	private static readonly JsonEncodedText PropName_showRunningIndicator = JsonEncodedText.Encode("showRunningIndicator");

	private static readonly JsonEncodedText PropName_onboardingDismissed = JsonEncodedText.Encode("onboardingDismissed");

	private static readonly JsonEncodedText PropName_updateNoticeSnoozedUntilUtc = JsonEncodedText.Encode("updateNoticeSnoozedUntilUtc");

	private static readonly JsonEncodedText PropName_appearance = JsonEncodedText.Encode("appearance");

	private static readonly JsonEncodedText PropName_dock = JsonEncodedText.Encode("dock");

	private static readonly JsonEncodedText PropName_launcher = JsonEncodedText.Encode("launcher");

	private static readonly JsonEncodedText PropName_shortcuts = JsonEncodedText.Encode("shortcuts");

	private static readonly JsonEncodedText PropName_display = JsonEncodedText.Encode("display");

	private static readonly JsonEncodedText PropName_performance = JsonEncodedText.Encode("performance");

	private static readonly JsonEncodedText PropName_desktopTaskbar = JsonEncodedText.Encode("desktopTaskbar");

	private static readonly JsonEncodedText PropName_enabled = JsonEncodedText.Encode("enabled");

	private static readonly JsonEncodedText PropName_control = JsonEncodedText.Encode("control");

	private static readonly JsonEncodedText PropName_alt = JsonEncodedText.Encode("alt");

	private static readonly JsonEncodedText PropName_shift = JsonEncodedText.Encode("shift");

	private static readonly JsonEncodedText PropName_win = JsonEncodedText.Encode("win");

	private static readonly JsonEncodedText PropName_key = JsonEncodedText.Encode("key");

	private static readonly JsonEncodedText PropName_isBound = JsonEncodedText.Encode("isBound");

	private static readonly JsonEncodedText PropName_groupModeEnabled = JsonEncodedText.Encode("groupModeEnabled");

	private static readonly JsonEncodedText PropName_rows = JsonEncodedText.Encode("rows");

	private static readonly JsonEncodedText PropName_columns = JsonEncodedText.Encode("columns");

	private static readonly JsonEncodedText PropName_windowWorkAreaRatio = JsonEncodedText.Encode("windowWorkAreaRatio");

	private static readonly JsonEncodedText PropName_fullscreenMode = JsonEncodedText.Encode("fullscreenMode");

	private static readonly JsonEncodedText PropName_focusSearchOnOpen = JsonEncodedText.Encode("focusSearchOnOpen");

	private static readonly JsonEncodedText PropName_rememberLastPage = JsonEncodedText.Encode("rememberLastPage");

	private static readonly JsonEncodedText PropName_highRefreshAnimationsEnabled = JsonEncodedText.Encode("highRefreshAnimationsEnabled");

	private static readonly JsonEncodedText PropName_autoSuspendWhenFullscreen = JsonEncodedText.Encode("autoSuspendWhenFullscreen");

	private static readonly JsonEncodedText PropName_mouseBackAction = JsonEncodedText.Encode("mouseBackAction");

	private static readonly JsonEncodedText PropName_mouseForwardAction = JsonEncodedText.Encode("mouseForwardAction");

	private static readonly JsonEncodedText PropName_toggleLauncher = JsonEncodedText.Encode("toggleLauncher");

	private static readonly JsonEncodedText PropName_hideLauncher = JsonEncodedText.Encode("hideLauncher");

	private static readonly JsonEncodedText PropName_nextPage = JsonEncodedText.Encode("nextPage");

	private static readonly JsonEncodedText PropName_previousPage = JsonEncodedText.Encode("previousPage");

	private static readonly JsonEncodedText PropName_toggleDockRevealLock = JsonEncodedText.Encode("toggleDockRevealLock");

	private static readonly JsonEncodedText PropName_toggleGameSleep = JsonEncodedText.Encode("toggleGameSleep");

	private static readonly JsonEncodedText PropName_hotCornersEnabled = JsonEncodedText.Encode("hotCornersEnabled");

	private static readonly JsonEncodedText PropName_hotCornerTarget = JsonEncodedText.Encode("hotCornerTarget");

	private static readonly JsonEncodedText PropName_hotCornerDwellMs = JsonEncodedText.Encode("hotCornerDwellMs");

	private static readonly JsonEncodedText PropName_mouseSideButtonsEnabled = JsonEncodedText.Encode("mouseSideButtonsEnabled");

	private static readonly JsonEncodedText PropName_mouseBackButton = JsonEncodedText.Encode("mouseBackButton");

	private static readonly JsonEncodedText PropName_mouseForwardButton = JsonEncodedText.Encode("mouseForwardButton");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<bool?> NullableBoolean => _NullableBoolean ?? (_NullableBoolean = (JsonTypeInfo<bool?>)base.Options.GetTypeInfo(typeof(bool?)));

	public JsonTypeInfo<double> Double => _Double ?? (_Double = (JsonTypeInfo<double>)base.Options.GetTypeInfo(typeof(double)));

	public JsonTypeInfo<InputAction> InputAction => _InputAction ?? (_InputAction = (JsonTypeInfo<InputAction>)base.Options.GetTypeInfo(typeof(InputAction)));

	public JsonTypeInfo<AppEntry> AppEntry => _AppEntry ?? (_AppEntry = (JsonTypeInfo<AppEntry>)base.Options.GetTypeInfo(typeof(AppEntry)));

	public JsonTypeInfo<AppItemKind> AppItemKind => _AppItemKind ?? (_AppItemKind = (JsonTypeInfo<AppItemKind>)base.Options.GetTypeInfo(typeof(AppItemKind)));

	public JsonTypeInfo<AppLibraryData> AppLibraryData => _AppLibraryData ?? (_AppLibraryData = (JsonTypeInfo<AppLibraryData>)base.Options.GetTypeInfo(typeof(AppLibraryData)));

	public JsonTypeInfo<BackupManifest> BackupManifest => _BackupManifest ?? (_BackupManifest = (JsonTypeInfo<BackupManifest>)base.Options.GetTypeInfo(typeof(BackupManifest)));

	public JsonTypeInfo<DockLayout> DockLayout => _DockLayout ?? (_DockLayout = (JsonTypeInfo<DockLayout>)base.Options.GetTypeInfo(typeof(DockLayout)));

	public JsonTypeInfo<GlassMaterialMode> GlassMaterialMode => _GlassMaterialMode ?? (_GlassMaterialMode = (JsonTypeInfo<GlassMaterialMode>)base.Options.GetTypeInfo(typeof(GlassMaterialMode)));

	public JsonTypeInfo<LauncherGroup> LauncherGroup => _LauncherGroup ?? (_LauncherGroup = (JsonTypeInfo<LauncherGroup>)base.Options.GetTypeInfo(typeof(LauncherGroup)));

	public JsonTypeInfo<LauncherLayout> LauncherLayout => _LauncherLayout ?? (_LauncherLayout = (JsonTypeInfo<LauncherLayout>)base.Options.GetTypeInfo(typeof(LauncherLayout)));

	public JsonTypeInfo<LauncherPage> LauncherPage => _LauncherPage ?? (_LauncherPage = (JsonTypeInfo<LauncherPage>)base.Options.GetTypeInfo(typeof(LauncherPage)));

	public JsonTypeInfo<LauncherSlot> LauncherSlot => _LauncherSlot ?? (_LauncherSlot = (JsonTypeInfo<LauncherSlot>)base.Options.GetTypeInfo(typeof(LauncherSlot)));

	public JsonTypeInfo<LauncherSlotKind> LauncherSlotKind => _LauncherSlotKind ?? (_LauncherSlotKind = (JsonTypeInfo<LauncherSlotKind>)base.Options.GetTypeInfo(typeof(LauncherSlotKind)));

	public JsonTypeInfo<AppearanceSettings> AppearanceSettings => _AppearanceSettings ?? (_AppearanceSettings = (JsonTypeInfo<AppearanceSettings>)base.Options.GetTypeInfo(typeof(AppearanceSettings)));

	public JsonTypeInfo<DesktopTaskbarSettings> DesktopTaskbarSettings => _DesktopTaskbarSettings ?? (_DesktopTaskbarSettings = (JsonTypeInfo<DesktopTaskbarSettings>)base.Options.GetTypeInfo(typeof(DesktopTaskbarSettings)));

	public JsonTypeInfo<DisplaySettings> DisplaySettings => _DisplaySettings ?? (_DisplaySettings = (JsonTypeInfo<DisplaySettings>)base.Options.GetTypeInfo(typeof(DisplaySettings)));

	public JsonTypeInfo<DockPlacement> DockPlacement => _DockPlacement ?? (_DockPlacement = (JsonTypeInfo<DockPlacement>)base.Options.GetTypeInfo(typeof(DockPlacement)));

	public JsonTypeInfo<DockSettings> DockSettings => _DockSettings ?? (_DockSettings = (JsonTypeInfo<DockSettings>)base.Options.GetTypeInfo(typeof(DockSettings)));

	public JsonTypeInfo<GlassDeskSettings> GlassDeskSettings => _GlassDeskSettings ?? (_GlassDeskSettings = (JsonTypeInfo<GlassDeskSettings>)base.Options.GetTypeInfo(typeof(GlassDeskSettings)));

	public JsonTypeInfo<HotCorner> HotCorner => _HotCorner ?? (_HotCorner = (JsonTypeInfo<HotCorner>)base.Options.GetTypeInfo(typeof(HotCorner)));

	public JsonTypeInfo<HotkeyDefinition> HotkeyDefinition => _HotkeyDefinition ?? (_HotkeyDefinition = (JsonTypeInfo<HotkeyDefinition>)base.Options.GetTypeInfo(typeof(HotkeyDefinition)));

	public JsonTypeInfo<LauncherSettings> LauncherSettings => _LauncherSettings ?? (_LauncherSettings = (JsonTypeInfo<LauncherSettings>)base.Options.GetTypeInfo(typeof(LauncherSettings)));

	public JsonTypeInfo<PerformanceSettings> PerformanceSettings => _PerformanceSettings ?? (_PerformanceSettings = (JsonTypeInfo<PerformanceSettings>)base.Options.GetTypeInfo(typeof(PerformanceSettings)));

	public JsonTypeInfo<ShortcutSettings> ShortcutSettings => _ShortcutSettings ?? (_ShortcutSettings = (JsonTypeInfo<ShortcutSettings>)base.Options.GetTypeInfo(typeof(ShortcutSettings)));

	public JsonTypeInfo<StatusBarEdge> StatusBarEdge => _StatusBarEdge ?? (_StatusBarEdge = (JsonTypeInfo<StatusBarEdge>)base.Options.GetTypeInfo(typeof(StatusBarEdge)));

	public JsonTypeInfo<ThemeMode> ThemeMode => _ThemeMode ?? (_ThemeMode = (JsonTypeInfo<ThemeMode>)base.Options.GetTypeInfo(typeof(ThemeMode)));

	public JsonTypeInfo<List<AppEntry>> ListAppEntry => _ListAppEntry ?? (_ListAppEntry = (JsonTypeInfo<List<AppEntry>>)base.Options.GetTypeInfo(typeof(List<AppEntry>)));

	public JsonTypeInfo<List<LauncherGroup>> ListLauncherGroup => _ListLauncherGroup ?? (_ListLauncherGroup = (JsonTypeInfo<List<LauncherGroup>>)base.Options.GetTypeInfo(typeof(List<LauncherGroup>)));

	public JsonTypeInfo<List<LauncherPage>> ListLauncherPage => _ListLauncherPage ?? (_ListLauncherPage = (JsonTypeInfo<List<LauncherPage>>)base.Options.GetTypeInfo(typeof(List<LauncherPage>)));

	public JsonTypeInfo<List<LauncherSlot>> ListLauncherSlot => _ListLauncherSlot ?? (_ListLauncherSlot = (JsonTypeInfo<List<LauncherSlot>>)base.Options.GetTypeInfo(typeof(List<LauncherSlot>)));

	public JsonTypeInfo<List<string>> ListString => _ListString ?? (_ListString = (JsonTypeInfo<List<string>>)base.Options.GetTypeInfo(typeof(List<string>)));

	public JsonTypeInfo<DateTimeOffset> DateTimeOffset => _DateTimeOffset ?? (_DateTimeOffset = (JsonTypeInfo<DateTimeOffset>)base.Options.GetTypeInfo(typeof(DateTimeOffset)));

	public JsonTypeInfo<DateTimeOffset?> NullableDateTimeOffset => _NullableDateTimeOffset ?? (_NullableDateTimeOffset = (JsonTypeInfo<DateTimeOffset?>)base.Options.GetTypeInfo(typeof(DateTimeOffset?)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static GlassDeskJsonContext Default { get; } = new GlassDeskJsonContext(new JsonSerializerOptions(s_defaultOptions));

	protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

	private JsonTypeInfo<bool> Create_Boolean(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<bool> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<bool>(options, JsonMetadataServices.BooleanConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<bool?> Create_NullableBoolean(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<bool?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<bool>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<bool?>(options, nullableConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<double> Create_Double(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<double> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<double>(options, JsonMetadataServices.DoubleConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<InputAction> Create_InputAction(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<InputAction> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(InputAction), new JsonStringEnumConverter<InputAction>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<InputAction>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<AppEntry> Create_AppEntry(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppEntry> jsonTypeInfo))
		{
			JsonObjectInfoValues<AppEntry> objectInfo = new JsonObjectInfoValues<AppEntry>
			{
				ObjectCreator = () => new AppEntry(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => AppEntryPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(AppEntry).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = AppEntrySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AppEntryPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[16];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).Id,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).Id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).DisplayName,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).DisplayName = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DisplayName",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("DisplayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<AppItemKind> propertyInfo3 = new JsonPropertyInfoValues<AppItemKind>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).Kind,
			Setter = delegate(object obj, AppItemKind value)
			{
				((AppEntry)obj).Kind = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Kind",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(AppItemKind), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).Target,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).Target = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Target",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).Arguments,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).Arguments = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Arguments",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("Arguments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).WorkingDirectory,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).WorkingDirectory = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WorkingDirectory",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("WorkingDirectory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).AppUserModelId,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).AppUserModelId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AppUserModelId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("AppUserModelId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<string> propertyInfo8 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).IconSource,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).IconSource = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IconSource",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("IconSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<string> propertyInfo9 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).CustomIconPath,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).CustomIconPath = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CustomIconPath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("CustomIconPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<List<string>> propertyInfo10 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).SearchAliases,
			Setter = delegate(object obj, List<string>? value)
			{
				((AppEntry)obj).SearchAliases = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SearchAliases",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("SearchAliases", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		array[9].IsGetNullable = false;
		array[9].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo11 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).PinyinIndex,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).PinyinIndex = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PinyinIndex",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("PinyinIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).PinyinInitials,
			Setter = delegate(object obj, string? value)
			{
				((AppEntry)obj).PinyinInitials = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PinyinInitials",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("PinyinInitials", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<bool> propertyInfo13 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).IsTargetMissing,
			Setter = delegate(object obj, bool value)
			{
				((AppEntry)obj).IsTargetMissing = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsTargetMissing",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("IsTargetMissing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo14 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).AddedAtUtc,
			Setter = delegate(object obj, DateTimeOffset value)
			{
				((AppEntry)obj).AddedAtUtc = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AddedAtUtc",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("AddedAtUtc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		JsonPropertyInfoValues<bool> propertyInfo15 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).HasLaunchTarget,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HasLaunchTarget",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("HasLaunchTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		JsonPropertyInfoValues<bool> propertyInfo16 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppEntry),
			Converter = null,
			Getter = (object obj) => ((AppEntry)obj).IsUsable,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsUsable",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppEntry).GetProperty("IsUsable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		return array;
	}

	private void AppEntrySerializeHandler(Utf8JsonWriter writer, AppEntry? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string id = value.Id;
		if (id != null)
		{
			writer.WriteString(PropName_id, id);
		}
		string displayName = value.DisplayName;
		if (displayName != null)
		{
			writer.WriteString(PropName_displayName, displayName);
		}
		writer.WritePropertyName(PropName_kind);
		JsonSerializer.Serialize(writer, value.Kind, AppItemKind);
		string target = value.Target;
		if (target != null)
		{
			writer.WriteString(PropName_target, target);
		}
		string arguments = value.Arguments;
		if (arguments != null)
		{
			writer.WriteString(PropName_arguments, arguments);
		}
		string workingDirectory = value.WorkingDirectory;
		if (workingDirectory != null)
		{
			writer.WriteString(PropName_workingDirectory, workingDirectory);
		}
		string appUserModelId = value.AppUserModelId;
		if (appUserModelId != null)
		{
			writer.WriteString(PropName_appUserModelId, appUserModelId);
		}
		string iconSource = value.IconSource;
		if (iconSource != null)
		{
			writer.WriteString(PropName_iconSource, iconSource);
		}
		string customIconPath = value.CustomIconPath;
		if (customIconPath != null)
		{
			writer.WriteString(PropName_customIconPath, customIconPath);
		}
		List<string> searchAliases = value.SearchAliases;
		if (searchAliases != null)
		{
			writer.WritePropertyName(PropName_searchAliases);
			ListStringSerializeHandler(writer, searchAliases);
		}
		string pinyinIndex = value.PinyinIndex;
		if (pinyinIndex != null)
		{
			writer.WriteString(PropName_pinyinIndex, pinyinIndex);
		}
		string pinyinInitials = value.PinyinInitials;
		if (pinyinInitials != null)
		{
			writer.WriteString(PropName_pinyinInitials, pinyinInitials);
		}
		writer.WriteBoolean(PropName_isTargetMissing, value.IsTargetMissing);
		writer.WriteString(PropName_addedAtUtc, value.AddedAtUtc);
		writer.WriteBoolean(PropName_hasLaunchTarget, value.HasLaunchTarget);
		writer.WriteBoolean(PropName_isUsable, value.IsUsable);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<AppItemKind> Create_AppItemKind(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppItemKind> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(AppItemKind), new JsonStringEnumConverter<AppItemKind>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<AppItemKind>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<AppLibraryData> Create_AppLibraryData(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppLibraryData> jsonTypeInfo))
		{
			JsonObjectInfoValues<AppLibraryData> objectInfo = new JsonObjectInfoValues<AppLibraryData>
			{
				ObjectCreator = () => new AppLibraryData(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => AppLibraryDataPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(AppLibraryData).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = AppLibraryDataSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AppLibraryDataPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppLibraryData),
			Converter = null,
			Getter = (object obj) => ((AppLibraryData)obj).SchemaVersion,
			Setter = delegate(object obj, int value)
			{
				((AppLibraryData)obj).SchemaVersion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SchemaVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppLibraryData).GetProperty("SchemaVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<List<AppEntry>> propertyInfo2 = new JsonPropertyInfoValues<List<AppEntry>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppLibraryData),
			Converter = null,
			Getter = (object obj) => ((AppLibraryData)obj).Items,
			Setter = delegate(object obj, List<AppEntry>? value)
			{
				((AppLibraryData)obj).Items = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Items",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppLibraryData).GetProperty("Items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<AppEntry>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		return array;
	}

	private void AppLibraryDataSerializeHandler(Utf8JsonWriter writer, AppLibraryData? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_schemaVersion, value.SchemaVersion);
		List<AppEntry> items = value.Items;
		if (items != null)
		{
			writer.WritePropertyName(PropName_items);
			ListAppEntrySerializeHandler(writer, items);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<BackupManifest> Create_BackupManifest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<BackupManifest> jsonTypeInfo))
		{
			JsonObjectInfoValues<BackupManifest> objectInfo = new JsonObjectInfoValues<BackupManifest>
			{
				ObjectCreator = () => new BackupManifest(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => BackupManifestPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(BackupManifest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = BackupManifestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] BackupManifestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[5];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(BackupManifest),
			Converter = null,
			Getter = (object obj) => ((BackupManifest)obj).Product,
			Setter = delegate(object obj, string? value)
			{
				((BackupManifest)obj).Product = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Product",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(BackupManifest).GetProperty("Product", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(BackupManifest),
			Converter = null,
			Getter = (object obj) => ((BackupManifest)obj).SchemaVersion,
			Setter = delegate(object obj, int value)
			{
				((BackupManifest)obj).SchemaVersion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SchemaVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(BackupManifest).GetProperty("SchemaVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(BackupManifest),
			Converter = null,
			Getter = (object obj) => ((BackupManifest)obj).ProductVersion,
			Setter = delegate(object obj, string? value)
			{
				((BackupManifest)obj).ProductVersion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProductVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(BackupManifest).GetProperty("ProductVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo4 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(BackupManifest),
			Converter = null,
			Getter = (object obj) => ((BackupManifest)obj).CreatedAtUtc,
			Setter = delegate(object obj, DateTimeOffset value)
			{
				((BackupManifest)obj).CreatedAtUtc = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CreatedAtUtc",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(BackupManifest).GetProperty("CreatedAtUtc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<List<string>> propertyInfo5 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(BackupManifest),
			Converter = null,
			Getter = (object obj) => ((BackupManifest)obj).Files,
			Setter = delegate(object obj, List<string>? value)
			{
				((BackupManifest)obj).Files = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Files",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(BackupManifest).GetProperty("Files", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		return array;
	}

	private void BackupManifestSerializeHandler(Utf8JsonWriter writer, BackupManifest? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string product = value.Product;
		if (product != null)
		{
			writer.WriteString(PropName_product, product);
		}
		writer.WriteNumber(PropName_schemaVersion, value.SchemaVersion);
		string productVersion = value.ProductVersion;
		if (productVersion != null)
		{
			writer.WriteString(PropName_productVersion, productVersion);
		}
		writer.WriteString(PropName_createdAtUtc, value.CreatedAtUtc);
		List<string> files = value.Files;
		if (files != null)
		{
			writer.WritePropertyName(PropName_files);
			ListStringSerializeHandler(writer, files);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<DockLayout> Create_DockLayout(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DockLayout> jsonTypeInfo))
		{
			JsonObjectInfoValues<DockLayout> objectInfo = new JsonObjectInfoValues<DockLayout>
			{
				ObjectCreator = () => new DockLayout(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => DockLayoutPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(DockLayout).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = DockLayoutSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DockLayoutPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<List<string>> propertyInfo = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockLayout),
			Converter = null,
			Getter = (object obj) => ((DockLayout)obj).PinnedItemIds,
			Setter = delegate(object obj, List<string>? value)
			{
				((DockLayout)obj).PinnedItemIds = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PinnedItemIds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockLayout).GetProperty("PinnedItemIds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		return array;
	}

	private void DockLayoutSerializeHandler(Utf8JsonWriter writer, DockLayout? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		List<string> pinnedItemIds = value.PinnedItemIds;
		if (pinnedItemIds != null)
		{
			writer.WritePropertyName(PropName_pinnedItemIds);
			ListStringSerializeHandler(writer, pinnedItemIds);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<GlassMaterialMode> Create_GlassMaterialMode(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<GlassMaterialMode> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(GlassMaterialMode), new JsonStringEnumConverter<GlassMaterialMode>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<GlassMaterialMode>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<LauncherGroup> Create_LauncherGroup(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LauncherGroup> jsonTypeInfo))
		{
			JsonObjectInfoValues<LauncherGroup> objectInfo = new JsonObjectInfoValues<LauncherGroup>
			{
				ObjectCreator = () => new LauncherGroup(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => LauncherGroupPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(LauncherGroup).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = LauncherGroupSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] LauncherGroupPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherGroup),
			Converter = null,
			Getter = (object obj) => ((LauncherGroup)obj).Id,
			Setter = delegate(object obj, string? value)
			{
				((LauncherGroup)obj).Id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherGroup).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherGroup),
			Converter = null,
			Getter = (object obj) => ((LauncherGroup)obj).Name,
			Setter = delegate(object obj, string? value)
			{
				((LauncherGroup)obj).Name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherGroup).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<List<string>> propertyInfo3 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherGroup),
			Converter = null,
			Getter = (object obj) => ((LauncherGroup)obj).ItemIds,
			Setter = delegate(object obj, List<string>? value)
			{
				((LauncherGroup)obj).ItemIds = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ItemIds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherGroup).GetProperty("ItemIds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		return array;
	}

	private void LauncherGroupSerializeHandler(Utf8JsonWriter writer, LauncherGroup? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string id = value.Id;
		if (id != null)
		{
			writer.WriteString(PropName_id, id);
		}
		string name = value.Name;
		if (name != null)
		{
			writer.WriteString(PropName_name, name);
		}
		List<string> itemIds = value.ItemIds;
		if (itemIds != null)
		{
			writer.WritePropertyName(PropName_itemIds);
			ListStringSerializeHandler(writer, itemIds);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<LauncherLayout> Create_LauncherLayout(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LauncherLayout> jsonTypeInfo))
		{
			JsonObjectInfoValues<LauncherLayout> objectInfo = new JsonObjectInfoValues<LauncherLayout>
			{
				ObjectCreator = () => new LauncherLayout(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => LauncherLayoutPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(LauncherLayout).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = LauncherLayoutSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] LauncherLayoutPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[6];
		JsonPropertyInfoValues<List<LauncherPage>> propertyInfo = new JsonPropertyInfoValues<List<LauncherPage>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherLayout),
			Converter = null,
			Getter = (object obj) => ((LauncherLayout)obj).Pages,
			Setter = delegate(object obj, List<LauncherPage>? value)
			{
				((LauncherLayout)obj).Pages = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Pages",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherLayout).GetProperty("Pages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<LauncherPage>), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<List<string>> propertyInfo2 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherLayout),
			Converter = null,
			Getter = (object obj) => ((LauncherLayout)obj).FlatOrder,
			Setter = delegate(object obj, List<string>? value)
			{
				((LauncherLayout)obj).FlatOrder = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FlatOrder",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherLayout).GetProperty("FlatOrder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		JsonPropertyInfoValues<List<LauncherGroup>> propertyInfo3 = new JsonPropertyInfoValues<List<LauncherGroup>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherLayout),
			Converter = null,
			Getter = (object obj) => ((LauncherLayout)obj).FilterGroups,
			Setter = delegate(object obj, List<LauncherGroup>? value)
			{
				((LauncherLayout)obj).FilterGroups = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FilterGroups",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherLayout).GetProperty("FilterGroups", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<LauncherGroup>), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<int> propertyInfo4 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherLayout),
			Converter = null,
			Getter = (object obj) => ((LauncherLayout)obj).SchemaVersion,
			Setter = delegate(object obj, int value)
			{
				((LauncherLayout)obj).SchemaVersion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SchemaVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherLayout).GetProperty("SchemaVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<List<string>> propertyInfo5 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherLayout),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ExcludedAppIds",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherLayout).GetProperty("ExcludedAppIds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<List<string>> propertyInfo6 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherLayout),
			Converter = null,
			Getter = (object obj) => ((LauncherLayout)obj).LegacyExcludedAppIds,
			Setter = delegate(object obj, List<string>? value)
			{
				((LauncherLayout)obj).LegacyExcludedAppIds = value;
			},
			IgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LegacyExcludedAppIds",
			JsonPropertyName = "excludedAppIds",
			AttributeProviderFactory = () => typeof(LauncherLayout).GetProperty("LegacyExcludedAppIds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		return array;
	}

	private void LauncherLayoutSerializeHandler(Utf8JsonWriter writer, LauncherLayout? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		List<LauncherPage> pages = value.Pages;
		if (pages != null)
		{
			writer.WritePropertyName(PropName_pages);
			ListLauncherPageSerializeHandler(writer, pages);
		}
		List<string> flatOrder = value.FlatOrder;
		if (flatOrder != null)
		{
			writer.WritePropertyName(PropName_flatOrder);
			ListStringSerializeHandler(writer, flatOrder);
		}
		List<LauncherGroup> filterGroups = value.FilterGroups;
		if (filterGroups != null)
		{
			writer.WritePropertyName(PropName_filterGroups);
			ListLauncherGroupSerializeHandler(writer, filterGroups);
		}
		writer.WriteNumber(PropName_schemaVersion, value.SchemaVersion);
		List<string> legacyExcludedAppIds = value.LegacyExcludedAppIds;
		if (legacyExcludedAppIds != null)
		{
			writer.WritePropertyName(PropName_excludedAppIds);
			ListStringSerializeHandler(writer, legacyExcludedAppIds);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<LauncherPage> Create_LauncherPage(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LauncherPage> jsonTypeInfo))
		{
			JsonObjectInfoValues<LauncherPage> objectInfo = new JsonObjectInfoValues<LauncherPage>
			{
				ObjectCreator = () => new LauncherPage(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => LauncherPagePropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(LauncherPage).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = LauncherPageSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] LauncherPagePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherPage),
			Converter = null,
			Getter = (object obj) => ((LauncherPage)obj).Id,
			Setter = delegate(object obj, string? value)
			{
				((LauncherPage)obj).Id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherPage).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<List<LauncherSlot>> propertyInfo2 = new JsonPropertyInfoValues<List<LauncherSlot>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherPage),
			Converter = null,
			Getter = (object obj) => ((LauncherPage)obj).Slots,
			Setter = delegate(object obj, List<LauncherSlot>? value)
			{
				((LauncherPage)obj).Slots = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Slots",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherPage).GetProperty("Slots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<LauncherSlot>), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].IsGetNullable = false;
		array[1].IsSetNullable = false;
		return array;
	}

	private void LauncherPageSerializeHandler(Utf8JsonWriter writer, LauncherPage? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string id = value.Id;
		if (id != null)
		{
			writer.WriteString(PropName_id, id);
		}
		List<LauncherSlot> slots = value.Slots;
		if (slots != null)
		{
			writer.WritePropertyName(PropName_slots);
			ListLauncherSlotSerializeHandler(writer, slots);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<LauncherSlot> Create_LauncherSlot(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LauncherSlot> jsonTypeInfo))
		{
			JsonObjectInfoValues<LauncherSlot> objectInfo = new JsonObjectInfoValues<LauncherSlot>
			{
				ObjectCreator = () => new LauncherSlot(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => LauncherSlotPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(LauncherSlot).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = LauncherSlotSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] LauncherSlotPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<LauncherSlotKind> propertyInfo = new JsonPropertyInfoValues<LauncherSlotKind>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSlot),
			Converter = null,
			Getter = (object obj) => ((LauncherSlot)obj).Kind,
			Setter = delegate(object obj, LauncherSlotKind value)
			{
				((LauncherSlot)obj).Kind = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Kind",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSlot).GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(LauncherSlotKind), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSlot),
			Converter = null,
			Getter = (object obj) => ((LauncherSlot)obj).ItemId,
			Setter = delegate(object obj, string? value)
			{
				((LauncherSlot)obj).ItemId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ItemId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSlot).GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<LauncherGroup> propertyInfo3 = new JsonPropertyInfoValues<LauncherGroup>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSlot),
			Converter = null,
			Getter = (object obj) => ((LauncherSlot)obj).Group,
			Setter = delegate(object obj, LauncherGroup? value)
			{
				((LauncherSlot)obj).Group = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Group",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSlot).GetProperty("Group", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(LauncherGroup), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void LauncherSlotSerializeHandler(Utf8JsonWriter writer, LauncherSlot? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WritePropertyName(PropName_kind);
		JsonSerializer.Serialize(writer, value.Kind, LauncherSlotKind);
		string itemId = value.ItemId;
		if (itemId != null)
		{
			writer.WriteString(PropName_itemId, itemId);
		}
		LauncherGroup launcherGroup = value.Group;
		if (launcherGroup != null)
		{
			writer.WritePropertyName(PropName_group);
			LauncherGroupSerializeHandler(writer, launcherGroup);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<LauncherSlotKind> Create_LauncherSlotKind(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LauncherSlotKind> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(LauncherSlotKind), new JsonStringEnumConverter<LauncherSlotKind>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<LauncherSlotKind>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<AppearanceSettings> Create_AppearanceSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AppearanceSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<AppearanceSettings> objectInfo = new JsonObjectInfoValues<AppearanceSettings>
			{
				ObjectCreator = () => new AppearanceSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => AppearanceSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(AppearanceSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = AppearanceSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AppearanceSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[18];
		JsonPropertyInfoValues<bool> propertyInfo = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).PinBarsToDesktop,
			Setter = delegate(object obj, bool value)
			{
				((AppearanceSettings)obj).PinBarsToDesktop = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PinBarsToDesktop",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("PinBarsToDesktop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<double> propertyInfo2 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).DockGlassTransparency,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).DockGlassTransparency = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DockGlassTransparency",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("DockGlassTransparency", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<double> propertyInfo3 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).LauncherGlassTransparency,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).LauncherGlassTransparency = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LauncherGlassTransparency",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("LauncherGlassTransparency", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<double> propertyInfo4 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).DockBuiltinIconBrightness,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).DockBuiltinIconBrightness = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DockBuiltinIconBrightness",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("DockBuiltinIconBrightness", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<bool> propertyInfo5 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).LaunchpadIconFilled,
			Setter = delegate(object obj, bool value)
			{
				((AppearanceSettings)obj).LaunchpadIconFilled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LaunchpadIconFilled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("LaunchpadIconFilled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<double> propertyInfo6 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).LauncherTextBrightness,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).LauncherTextBrightness = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LauncherTextBrightness",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("LauncherTextBrightness", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<double> propertyInfo7 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).StatusBarTextBrightness,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).StatusBarTextBrightness = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StatusBarTextBrightness",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("StatusBarTextBrightness", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<double> propertyInfo8 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).GlassBackgroundClarity,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).GlassBackgroundClarity = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "GlassBackgroundClarity",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("GlassBackgroundClarity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<double> propertyInfo9 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).GlassTintOpacity,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).GlassTintOpacity = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "GlassTintOpacity",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("GlassTintOpacity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<GlassMaterialMode> propertyInfo10 = new JsonPropertyInfoValues<GlassMaterialMode>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).GlassMaterial,
			Setter = delegate(object obj, GlassMaterialMode value)
			{
				((AppearanceSettings)obj).GlassMaterial = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "GlassMaterial",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("GlassMaterial", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(GlassMaterialMode), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<ThemeMode> propertyInfo11 = new JsonPropertyInfoValues<ThemeMode>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).Theme,
			Setter = delegate(object obj, ThemeMode value)
			{
				((AppearanceSettings)obj).Theme = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Theme",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("Theme", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ThemeMode), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).AccentColor,
			Setter = delegate(object obj, string? value)
			{
				((AppearanceSettings)obj).AccentColor = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AccentColor",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("AccentColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		array[11].IsGetNullable = false;
		array[11].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo13 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).UseSystemBackdrop,
			Setter = delegate(object obj, bool value)
			{
				((AppearanceSettings)obj).UseSystemBackdrop = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UseSystemBackdrop",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("UseSystemBackdrop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<bool> propertyInfo14 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).ReduceMotion,
			Setter = delegate(object obj, bool value)
			{
				((AppearanceSettings)obj).ReduceMotion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ReduceMotion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("ReduceMotion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		JsonPropertyInfoValues<bool> propertyInfo15 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).LiveBackdrop,
			Setter = delegate(object obj, bool value)
			{
				((AppearanceSettings)obj).LiveBackdrop = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LiveBackdrop",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("LiveBackdrop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		JsonPropertyInfoValues<bool> propertyInfo16 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).ReduceTransparency,
			Setter = delegate(object obj, bool value)
			{
				((AppearanceSettings)obj).ReduceTransparency = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ReduceTransparency",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("ReduceTransparency", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		JsonPropertyInfoValues<double> propertyInfo17 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).DockCornerRadius,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).DockCornerRadius = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DockCornerRadius",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("DockCornerRadius", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
		JsonPropertyInfoValues<double> propertyInfo18 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AppearanceSettings),
			Converter = null,
			Getter = (object obj) => ((AppearanceSettings)obj).LauncherCornerRadius,
			Setter = delegate(object obj, double value)
			{
				((AppearanceSettings)obj).LauncherCornerRadius = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LauncherCornerRadius",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AppearanceSettings).GetProperty("LauncherCornerRadius", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
		return array;
	}

	private void AppearanceSettingsSerializeHandler(Utf8JsonWriter writer, AppearanceSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_pinBarsToDesktop, value.PinBarsToDesktop);
		writer.WriteNumber(PropName_dockGlassTransparency, value.DockGlassTransparency);
		writer.WriteNumber(PropName_launcherGlassTransparency, value.LauncherGlassTransparency);
		writer.WriteNumber(PropName_dockBuiltinIconBrightness, value.DockBuiltinIconBrightness);
		writer.WriteBoolean(PropName_launchpadIconFilled, value.LaunchpadIconFilled);
		writer.WriteNumber(PropName_launcherTextBrightness, value.LauncherTextBrightness);
		writer.WriteNumber(PropName_statusBarTextBrightness, value.StatusBarTextBrightness);
		writer.WriteNumber(PropName_glassBackgroundClarity, value.GlassBackgroundClarity);
		writer.WriteNumber(PropName_glassTintOpacity, value.GlassTintOpacity);
		writer.WritePropertyName(PropName_glassMaterial);
		JsonSerializer.Serialize(writer, value.GlassMaterial, GlassMaterialMode);
		writer.WritePropertyName(PropName_theme);
		JsonSerializer.Serialize(writer, value.Theme, ThemeMode);
		string accentColor = value.AccentColor;
		if (accentColor != null)
		{
			writer.WriteString(PropName_accentColor, accentColor);
		}
		writer.WriteBoolean(PropName_useSystemBackdrop, value.UseSystemBackdrop);
		writer.WriteBoolean(PropName_reduceMotion, value.ReduceMotion);
		writer.WriteBoolean(PropName_liveBackdrop, value.LiveBackdrop);
		writer.WriteBoolean(PropName_reduceTransparency, value.ReduceTransparency);
		writer.WriteNumber(PropName_dockCornerRadius, value.DockCornerRadius);
		writer.WriteNumber(PropName_launcherCornerRadius, value.LauncherCornerRadius);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<DesktopTaskbarSettings> Create_DesktopTaskbarSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DesktopTaskbarSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<DesktopTaskbarSettings> objectInfo = new JsonObjectInfoValues<DesktopTaskbarSettings>
			{
				ObjectCreator = () => new DesktopTaskbarSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => DesktopTaskbarSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(DesktopTaskbarSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = DesktopTaskbarSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DesktopTaskbarSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DesktopTaskbarSettings),
			Converter = null,
			Getter = (object obj) => ((DesktopTaskbarSettings)obj).ToggleDesktopIcons,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((DesktopTaskbarSettings)obj).ToggleDesktopIcons = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ToggleDesktopIcons",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DesktopTaskbarSettings).GetProperty("ToggleDesktopIcons", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		array[0].IsGetNullable = false;
		array[0].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DesktopTaskbarSettings),
			Converter = null,
			Getter = (object obj) => ((DesktopTaskbarSettings)obj).BlockTaskbarEdgeReveal,
			Setter = delegate(object obj, bool value)
			{
				((DesktopTaskbarSettings)obj).BlockTaskbarEdgeReveal = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BlockTaskbarEdgeReveal",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DesktopTaskbarSettings).GetProperty("BlockTaskbarEdgeReveal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DesktopTaskbarSettings),
			Converter = null,
			Getter = (object obj) => ((DesktopTaskbarSettings)obj).TaskbarAutoHideEnabled,
			Setter = delegate(object obj, bool value)
			{
				((DesktopTaskbarSettings)obj).TaskbarAutoHideEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TaskbarAutoHideEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DesktopTaskbarSettings).GetProperty("TaskbarAutoHideEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void DesktopTaskbarSettingsSerializeHandler(Utf8JsonWriter writer, DesktopTaskbarSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		HotkeyDefinition toggleDesktopIcons = value.ToggleDesktopIcons;
		if (toggleDesktopIcons != null)
		{
			writer.WritePropertyName(PropName_toggleDesktopIcons);
			HotkeyDefinitionSerializeHandler(writer, toggleDesktopIcons);
		}
		writer.WriteBoolean(PropName_blockTaskbarEdgeReveal, value.BlockTaskbarEdgeReveal);
		writer.WriteBoolean(PropName_taskbarAutoHideEnabled, value.TaskbarAutoHideEnabled);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<DisplaySettings> Create_DisplaySettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DisplaySettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<DisplaySettings> objectInfo = new JsonObjectInfoValues<DisplaySettings>
			{
				ObjectCreator = () => new DisplaySettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => DisplaySettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(DisplaySettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = DisplaySettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DisplaySettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DisplaySettings),
			Converter = null,
			Getter = (object obj) => ((DisplaySettings)obj).DockDisplayId,
			Setter = delegate(object obj, string? value)
			{
				((DisplaySettings)obj).DockDisplayId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DockDisplayId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DisplaySettings).GetProperty("DockDisplayId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DisplaySettings),
			Converter = null,
			Getter = (object obj) => ((DisplaySettings)obj).LauncherFollowsPointer,
			Setter = delegate(object obj, bool value)
			{
				((DisplaySettings)obj).LauncherFollowsPointer = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LauncherFollowsPointer",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DisplaySettings).GetProperty("LauncherFollowsPointer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DisplaySettings),
			Converter = null,
			Getter = (object obj) => ((DisplaySettings)obj).SuppressTriggersWhenFullscreen,
			Setter = delegate(object obj, bool value)
			{
				((DisplaySettings)obj).SuppressTriggersWhenFullscreen = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SuppressTriggersWhenFullscreen",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DisplaySettings).GetProperty("SuppressTriggersWhenFullscreen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void DisplaySettingsSerializeHandler(Utf8JsonWriter writer, DisplaySettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string dockDisplayId = value.DockDisplayId;
		if (dockDisplayId != null)
		{
			writer.WriteString(PropName_dockDisplayId, dockDisplayId);
		}
		writer.WriteBoolean(PropName_launcherFollowsPointer, value.LauncherFollowsPointer);
		writer.WriteBoolean(PropName_suppressTriggersWhenFullscreen, value.SuppressTriggersWhenFullscreen);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<DockPlacement> Create_DockPlacement(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DockPlacement> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(DockPlacement), new JsonStringEnumConverter<DockPlacement>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DockPlacement>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<DockSettings> Create_DockSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DockSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<DockSettings> objectInfo = new JsonObjectInfoValues<DockSettings>
			{
				ObjectCreator = () => new DockSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => DockSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(DockSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = DockSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DockSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[27];
		JsonPropertyInfoValues<double> propertyInfo = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).SizeScale,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).SizeScale = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SizeScale",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("SizeScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).ShowLauncherEntry,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).ShowLauncherEntry = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowLauncherEntry",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("ShowLauncherEntry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).ShowThisPcEntry,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).ShowThisPcEntry = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowThisPcEntry",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("ShowThisPcEntry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<bool> propertyInfo4 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).ShowDesktopIconsEntry,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).ShowDesktopIconsEntry = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowDesktopIconsEntry",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("ShowDesktopIconsEntry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).LauncherCustomIconPath,
			Setter = delegate(object obj, string? value)
			{
				((DockSettings)obj).LauncherCustomIconPath = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LauncherCustomIconPath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("LauncherCustomIconPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).ThisPcCustomIconPath,
			Setter = delegate(object obj, string? value)
			{
				((DockSettings)obj).ThisPcCustomIconPath = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ThisPcCustomIconPath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("ThisPcCustomIconPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).DesktopIconsCustomIconPath,
			Setter = delegate(object obj, string? value)
			{
				((DockSettings)obj).DesktopIconsCustomIconPath = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DesktopIconsCustomIconPath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("DesktopIconsCustomIconPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).ShowStatusBar,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).ShowStatusBar = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowStatusBar",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("ShowStatusBar", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<bool> propertyInfo9 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).ShowStatusBarNetworkSpeed,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).ShowStatusBarNetworkSpeed = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowStatusBarNetworkSpeed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("ShowStatusBarNetworkSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<double> propertyInfo10 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).StatusBarSizeScale,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).StatusBarSizeScale = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StatusBarSizeScale",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("StatusBarSizeScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<StatusBarEdge> propertyInfo11 = new JsonPropertyInfoValues<StatusBarEdge>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).StatusBarEdge,
			Setter = delegate(object obj, StatusBarEdge value)
			{
				((DockSettings)obj).StatusBarEdge = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StatusBarEdge",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("StatusBarEdge", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(StatusBarEdge), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<double> propertyInfo12 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).StatusBarOffsetX,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).StatusBarOffsetX = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StatusBarOffsetX",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("StatusBarOffsetX", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<double> propertyInfo13 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).StatusBarOffsetY,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).StatusBarOffsetY = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StatusBarOffsetY",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("StatusBarOffsetY", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<double> propertyInfo14 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).FrameLengthDip,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).FrameLengthDip = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FrameLengthDip",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("FrameLengthDip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		JsonPropertyInfoValues<double> propertyInfo15 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).IconSize,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).IconSize = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IconSize",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("IconSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		JsonPropertyInfoValues<double> propertyInfo16 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).IconSpacing,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).IconSpacing = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IconSpacing",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("IconSpacing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		JsonPropertyInfoValues<double> propertyInfo17 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).MagnificationPeak,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).MagnificationPeak = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MagnificationPeak",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("MagnificationPeak", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
		JsonPropertyInfoValues<DockPlacement> propertyInfo18 = new JsonPropertyInfoValues<DockPlacement>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).Placement,
			Setter = delegate(object obj, DockPlacement value)
			{
				((DockSettings)obj).Placement = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Placement",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("Placement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DockPlacement), Array.Empty<Type>(), null)
		};
		array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
		JsonPropertyInfoValues<double> propertyInfo19 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).OffsetX,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).OffsetX = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OffsetX",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("OffsetX", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[18] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo19);
		JsonPropertyInfoValues<double> propertyInfo20 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).OffsetY,
			Setter = delegate(object obj, double value)
			{
				((DockSettings)obj).OffsetY = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OffsetY",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("OffsetY", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[19] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo20);
		JsonPropertyInfoValues<bool> propertyInfo21 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).AutoHide,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).AutoHide = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AutoHide",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("AutoHide", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[20] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo21);
		JsonPropertyInfoValues<int> propertyInfo22 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).AutoHideLeaveDelayMs,
			Setter = delegate(object obj, int value)
			{
				((DockSettings)obj).AutoHideLeaveDelayMs = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AutoHideLeaveDelayMs",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("AutoHideLeaveDelayMs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[21] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo22);
		JsonPropertyInfoValues<bool> propertyInfo23 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).EdgeRevealEnabled,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).EdgeRevealEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "EdgeRevealEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("EdgeRevealEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[22] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo23);
		JsonPropertyInfoValues<int> propertyInfo24 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).EdgeHotzoneSize,
			Setter = delegate(object obj, int value)
			{
				((DockSettings)obj).EdgeHotzoneSize = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "EdgeHotzoneSize",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("EdgeHotzoneSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[23] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo24);
		JsonPropertyInfoValues<bool> propertyInfo25 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).BlockRevealWhenCovered,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).BlockRevealWhenCovered = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BlockRevealWhenCovered",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("BlockRevealWhenCovered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[24] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo25);
		JsonPropertyInfoValues<string> propertyInfo26 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).TargetDisplayId,
			Setter = delegate(object obj, string? value)
			{
				((DockSettings)obj).TargetDisplayId = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TargetDisplayId",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("TargetDisplayId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[25] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo26);
		JsonPropertyInfoValues<bool> propertyInfo27 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DockSettings),
			Converter = null,
			Getter = (object obj) => ((DockSettings)obj).ShowRunningIndicator,
			Setter = delegate(object obj, bool value)
			{
				((DockSettings)obj).ShowRunningIndicator = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ShowRunningIndicator",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DockSettings).GetProperty("ShowRunningIndicator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[26] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo27);
		return array;
	}

	private void DockSettingsSerializeHandler(Utf8JsonWriter writer, DockSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_sizeScale, value.SizeScale);
		writer.WriteBoolean(PropName_showLauncherEntry, value.ShowLauncherEntry);
		writer.WriteBoolean(PropName_showThisPcEntry, value.ShowThisPcEntry);
		writer.WriteBoolean(PropName_showDesktopIconsEntry, value.ShowDesktopIconsEntry);
		string launcherCustomIconPath = value.LauncherCustomIconPath;
		if (launcherCustomIconPath != null)
		{
			writer.WriteString(PropName_launcherCustomIconPath, launcherCustomIconPath);
		}
		string thisPcCustomIconPath = value.ThisPcCustomIconPath;
		if (thisPcCustomIconPath != null)
		{
			writer.WriteString(PropName_thisPcCustomIconPath, thisPcCustomIconPath);
		}
		string desktopIconsCustomIconPath = value.DesktopIconsCustomIconPath;
		if (desktopIconsCustomIconPath != null)
		{
			writer.WriteString(PropName_desktopIconsCustomIconPath, desktopIconsCustomIconPath);
		}
		writer.WriteBoolean(PropName_showStatusBar, value.ShowStatusBar);
		writer.WriteBoolean(PropName_showStatusBarNetworkSpeed, value.ShowStatusBarNetworkSpeed);
		writer.WriteNumber(PropName_statusBarSizeScale, value.StatusBarSizeScale);
		writer.WritePropertyName(PropName_statusBarEdge);
		JsonSerializer.Serialize(writer, value.StatusBarEdge, StatusBarEdge);
		writer.WriteNumber(PropName_statusBarOffsetX, value.StatusBarOffsetX);
		writer.WriteNumber(PropName_statusBarOffsetY, value.StatusBarOffsetY);
		writer.WriteNumber(PropName_frameLengthDip, value.FrameLengthDip);
		writer.WriteNumber(PropName_iconSize, value.IconSize);
		writer.WriteNumber(PropName_iconSpacing, value.IconSpacing);
		writer.WriteNumber(PropName_magnificationPeak, value.MagnificationPeak);
		writer.WritePropertyName(PropName_placement);
		JsonSerializer.Serialize(writer, value.Placement, DockPlacement);
		writer.WriteNumber(PropName_offsetX, value.OffsetX);
		writer.WriteNumber(PropName_offsetY, value.OffsetY);
		writer.WriteBoolean(PropName_autoHide, value.AutoHide);
		writer.WriteNumber(PropName_autoHideLeaveDelayMs, value.AutoHideLeaveDelayMs);
		writer.WriteBoolean(PropName_edgeRevealEnabled, value.EdgeRevealEnabled);
		writer.WriteNumber(PropName_edgeHotzoneSize, value.EdgeHotzoneSize);
		writer.WriteBoolean(PropName_blockRevealWhenCovered, value.BlockRevealWhenCovered);
		string targetDisplayId = value.TargetDisplayId;
		if (targetDisplayId != null)
		{
			writer.WriteString(PropName_targetDisplayId, targetDisplayId);
		}
		writer.WriteBoolean(PropName_showRunningIndicator, value.ShowRunningIndicator);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<GlassDeskSettings> Create_GlassDeskSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<GlassDeskSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<GlassDeskSettings> objectInfo = new JsonObjectInfoValues<GlassDeskSettings>
			{
				ObjectCreator = () => new GlassDeskSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => GlassDeskSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(GlassDeskSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = GlassDeskSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] GlassDeskSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[10];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).SchemaVersion,
			Setter = delegate(object obj, int value)
			{
				((GlassDeskSettings)obj).SchemaVersion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SchemaVersion",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("SchemaVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool?> propertyInfo2 = new JsonPropertyInfoValues<bool?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).OnboardingDismissed,
			Setter = delegate(object obj, bool? value)
			{
				((GlassDeskSettings)obj).OnboardingDismissed = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "OnboardingDismissed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("OnboardingDismissed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool?), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<DateTimeOffset?> propertyInfo3 = new JsonPropertyInfoValues<DateTimeOffset?>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).UpdateNoticeSnoozedUntilUtc,
			Setter = delegate(object obj, DateTimeOffset? value)
			{
				((GlassDeskSettings)obj).UpdateNoticeSnoozedUntilUtc = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "UpdateNoticeSnoozedUntilUtc",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("UpdateNoticeSnoozedUntilUtc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTimeOffset?), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<AppearanceSettings> propertyInfo4 = new JsonPropertyInfoValues<AppearanceSettings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).Appearance,
			Setter = delegate(object obj, AppearanceSettings? value)
			{
				((GlassDeskSettings)obj).Appearance = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Appearance",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("Appearance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(AppearanceSettings), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<DockSettings> propertyInfo5 = new JsonPropertyInfoValues<DockSettings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).Dock,
			Setter = delegate(object obj, DockSettings? value)
			{
				((GlassDeskSettings)obj).Dock = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Dock",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("Dock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DockSettings), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<LauncherSettings> propertyInfo6 = new JsonPropertyInfoValues<LauncherSettings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).Launcher,
			Setter = delegate(object obj, LauncherSettings? value)
			{
				((GlassDeskSettings)obj).Launcher = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Launcher",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("Launcher", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(LauncherSettings), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		JsonPropertyInfoValues<ShortcutSettings> propertyInfo7 = new JsonPropertyInfoValues<ShortcutSettings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).Shortcuts,
			Setter = delegate(object obj, ShortcutSettings? value)
			{
				((GlassDeskSettings)obj).Shortcuts = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Shortcuts",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("Shortcuts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ShortcutSettings), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		array[6].IsGetNullable = false;
		array[6].IsSetNullable = false;
		JsonPropertyInfoValues<DisplaySettings> propertyInfo8 = new JsonPropertyInfoValues<DisplaySettings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).Display,
			Setter = delegate(object obj, DisplaySettings? value)
			{
				((GlassDeskSettings)obj).Display = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Display",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("Display", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DisplaySettings), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		array[7].IsGetNullable = false;
		array[7].IsSetNullable = false;
		JsonPropertyInfoValues<PerformanceSettings> propertyInfo9 = new JsonPropertyInfoValues<PerformanceSettings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).Performance,
			Setter = delegate(object obj, PerformanceSettings? value)
			{
				((GlassDeskSettings)obj).Performance = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Performance",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("Performance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PerformanceSettings), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		array[8].IsGetNullable = false;
		array[8].IsSetNullable = false;
		JsonPropertyInfoValues<DesktopTaskbarSettings> propertyInfo10 = new JsonPropertyInfoValues<DesktopTaskbarSettings>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GlassDeskSettings),
			Converter = null,
			Getter = (object obj) => ((GlassDeskSettings)obj).DesktopTaskbar,
			Setter = delegate(object obj, DesktopTaskbarSettings? value)
			{
				((GlassDeskSettings)obj).DesktopTaskbar = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DesktopTaskbar",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GlassDeskSettings).GetProperty("DesktopTaskbar", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DesktopTaskbarSettings), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		array[9].IsGetNullable = false;
		array[9].IsSetNullable = false;
		return array;
	}

	private void GlassDeskSettingsSerializeHandler(Utf8JsonWriter writer, GlassDeskSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_schemaVersion, value.SchemaVersion);
		bool? onboardingDismissed = value.OnboardingDismissed;
		if (onboardingDismissed.HasValue)
		{
			writer.WritePropertyName(PropName_onboardingDismissed);
			JsonSerializer.Serialize(writer, onboardingDismissed, NullableBoolean);
		}
		DateTimeOffset? updateNoticeSnoozedUntilUtc = value.UpdateNoticeSnoozedUntilUtc;
		if (updateNoticeSnoozedUntilUtc.HasValue)
		{
			writer.WritePropertyName(PropName_updateNoticeSnoozedUntilUtc);
			JsonSerializer.Serialize(writer, updateNoticeSnoozedUntilUtc, NullableDateTimeOffset);
		}
		AppearanceSettings appearance = value.Appearance;
		if (appearance != null)
		{
			writer.WritePropertyName(PropName_appearance);
			AppearanceSettingsSerializeHandler(writer, appearance);
		}
		DockSettings dock = value.Dock;
		if (dock != null)
		{
			writer.WritePropertyName(PropName_dock);
			DockSettingsSerializeHandler(writer, dock);
		}
		LauncherSettings launcher = value.Launcher;
		if (launcher != null)
		{
			writer.WritePropertyName(PropName_launcher);
			LauncherSettingsSerializeHandler(writer, launcher);
		}
		ShortcutSettings shortcuts = value.Shortcuts;
		if (shortcuts != null)
		{
			writer.WritePropertyName(PropName_shortcuts);
			ShortcutSettingsSerializeHandler(writer, shortcuts);
		}
		DisplaySettings display = value.Display;
		if (display != null)
		{
			writer.WritePropertyName(PropName_display);
			DisplaySettingsSerializeHandler(writer, display);
		}
		PerformanceSettings performance = value.Performance;
		if (performance != null)
		{
			writer.WritePropertyName(PropName_performance);
			PerformanceSettingsSerializeHandler(writer, performance);
		}
		DesktopTaskbarSettings desktopTaskbar = value.DesktopTaskbar;
		if (desktopTaskbar != null)
		{
			writer.WritePropertyName(PropName_desktopTaskbar);
			DesktopTaskbarSettingsSerializeHandler(writer, desktopTaskbar);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<HotCorner> Create_HotCorner(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<HotCorner> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(HotCorner), new JsonStringEnumConverter<HotCorner>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<HotCorner>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<HotkeyDefinition> Create_HotkeyDefinition(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<HotkeyDefinition> jsonTypeInfo))
		{
			JsonObjectInfoValues<HotkeyDefinition> objectInfo = new JsonObjectInfoValues<HotkeyDefinition>
			{
				ObjectCreator = () => new HotkeyDefinition(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => HotkeyDefinitionPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(HotkeyDefinition).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = HotkeyDefinitionSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] HotkeyDefinitionPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<bool> propertyInfo = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyDefinition),
			Converter = null,
			Getter = (object obj) => ((HotkeyDefinition)obj).Enabled,
			Setter = delegate(object obj, bool value)
			{
				((HotkeyDefinition)obj).Enabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Enabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyDefinition).GetProperty("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyDefinition),
			Converter = null,
			Getter = (object obj) => ((HotkeyDefinition)obj).Control,
			Setter = delegate(object obj, bool value)
			{
				((HotkeyDefinition)obj).Control = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Control",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyDefinition).GetProperty("Control", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyDefinition),
			Converter = null,
			Getter = (object obj) => ((HotkeyDefinition)obj).Alt,
			Setter = delegate(object obj, bool value)
			{
				((HotkeyDefinition)obj).Alt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Alt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyDefinition).GetProperty("Alt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<bool> propertyInfo4 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyDefinition),
			Converter = null,
			Getter = (object obj) => ((HotkeyDefinition)obj).Shift,
			Setter = delegate(object obj, bool value)
			{
				((HotkeyDefinition)obj).Shift = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Shift",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyDefinition).GetProperty("Shift", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<bool> propertyInfo5 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyDefinition),
			Converter = null,
			Getter = (object obj) => ((HotkeyDefinition)obj).Win,
			Setter = delegate(object obj, bool value)
			{
				((HotkeyDefinition)obj).Win = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Win",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyDefinition).GetProperty("Win", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyDefinition),
			Converter = null,
			Getter = (object obj) => ((HotkeyDefinition)obj).Key,
			Setter = delegate(object obj, string? value)
			{
				((HotkeyDefinition)obj).Key = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Key",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyDefinition).GetProperty("Key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(HotkeyDefinition),
			Converter = null,
			Getter = (object obj) => ((HotkeyDefinition)obj).IsBound,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "IsBound",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(HotkeyDefinition).GetProperty("IsBound", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		return array;
	}

	private void HotkeyDefinitionSerializeHandler(Utf8JsonWriter writer, HotkeyDefinition? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_enabled, value.Enabled);
		writer.WriteBoolean(PropName_control, value.Control);
		writer.WriteBoolean(PropName_alt, value.Alt);
		writer.WriteBoolean(PropName_shift, value.Shift);
		writer.WriteBoolean(PropName_win, value.Win);
		string key = value.Key;
		if (key != null)
		{
			writer.WriteString(PropName_key, key);
		}
		writer.WriteBoolean(PropName_isBound, value.IsBound);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<LauncherSettings> Create_LauncherSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LauncherSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<LauncherSettings> objectInfo = new JsonObjectInfoValues<LauncherSettings>
			{
				ObjectCreator = () => new LauncherSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => LauncherSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(LauncherSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = LauncherSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] LauncherSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[8];
		JsonPropertyInfoValues<bool> propertyInfo = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).GroupModeEnabled,
			Setter = delegate(object obj, bool value)
			{
				((LauncherSettings)obj).GroupModeEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "GroupModeEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("GroupModeEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).Rows,
			Setter = delegate(object obj, int value)
			{
				((LauncherSettings)obj).Rows = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Rows",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("Rows", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<int> propertyInfo3 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).Columns,
			Setter = delegate(object obj, int value)
			{
				((LauncherSettings)obj).Columns = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Columns",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("Columns", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<double> propertyInfo4 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).WindowWorkAreaRatio,
			Setter = delegate(object obj, double value)
			{
				((LauncherSettings)obj).WindowWorkAreaRatio = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "WindowWorkAreaRatio",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("WindowWorkAreaRatio", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<bool> propertyInfo5 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).FullscreenMode,
			Setter = delegate(object obj, bool value)
			{
				((LauncherSettings)obj).FullscreenMode = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FullscreenMode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("FullscreenMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<bool> propertyInfo6 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).FocusSearchOnOpen,
			Setter = delegate(object obj, bool value)
			{
				((LauncherSettings)obj).FocusSearchOnOpen = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FocusSearchOnOpen",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("FocusSearchOnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<bool> propertyInfo7 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).RememberLastPage,
			Setter = delegate(object obj, bool value)
			{
				((LauncherSettings)obj).RememberLastPage = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RememberLastPage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("RememberLastPage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(LauncherSettings),
			Converter = null,
			Getter = (object obj) => ((LauncherSettings)obj).HighRefreshAnimationsEnabled,
			Setter = delegate(object obj, bool value)
			{
				((LauncherSettings)obj).HighRefreshAnimationsEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HighRefreshAnimationsEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(LauncherSettings).GetProperty("HighRefreshAnimationsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		return array;
	}

	private void LauncherSettingsSerializeHandler(Utf8JsonWriter writer, LauncherSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_groupModeEnabled, value.GroupModeEnabled);
		writer.WriteNumber(PropName_rows, value.Rows);
		writer.WriteNumber(PropName_columns, value.Columns);
		writer.WriteNumber(PropName_windowWorkAreaRatio, value.WindowWorkAreaRatio);
		writer.WriteBoolean(PropName_fullscreenMode, value.FullscreenMode);
		writer.WriteBoolean(PropName_focusSearchOnOpen, value.FocusSearchOnOpen);
		writer.WriteBoolean(PropName_rememberLastPage, value.RememberLastPage);
		writer.WriteBoolean(PropName_highRefreshAnimationsEnabled, value.HighRefreshAnimationsEnabled);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<PerformanceSettings> Create_PerformanceSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<PerformanceSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<PerformanceSettings> objectInfo = new JsonObjectInfoValues<PerformanceSettings>
			{
				ObjectCreator = () => new PerformanceSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => PerformanceSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(PerformanceSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = PerformanceSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] PerformanceSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<bool> propertyInfo = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(PerformanceSettings),
			Converter = null,
			Getter = (object obj) => ((PerformanceSettings)obj).AutoSuspendWhenFullscreen,
			Setter = delegate(object obj, bool value)
			{
				((PerformanceSettings)obj).AutoSuspendWhenFullscreen = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "AutoSuspendWhenFullscreen",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(PerformanceSettings).GetProperty("AutoSuspendWhenFullscreen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		return array;
	}

	private void PerformanceSettingsSerializeHandler(Utf8JsonWriter writer, PerformanceSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_autoSuspendWhenFullscreen, value.AutoSuspendWhenFullscreen);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ShortcutSettings> Create_ShortcutSettings(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ShortcutSettings> jsonTypeInfo))
		{
			JsonObjectInfoValues<ShortcutSettings> objectInfo = new JsonObjectInfoValues<ShortcutSettings>
			{
				ObjectCreator = () => new ShortcutSettings(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ShortcutSettingsPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ShortcutSettings).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ShortcutSettingsSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ShortcutSettingsPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[15];
		JsonPropertyInfoValues<InputAction> propertyInfo = new JsonPropertyInfoValues<InputAction>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).MouseBackAction,
			Setter = delegate(object obj, InputAction value)
			{
				((ShortcutSettings)obj).MouseBackAction = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MouseBackAction",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("MouseBackAction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(InputAction), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<InputAction> propertyInfo2 = new JsonPropertyInfoValues<InputAction>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).MouseForwardAction,
			Setter = delegate(object obj, InputAction value)
			{
				((ShortcutSettings)obj).MouseForwardAction = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MouseForwardAction",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("MouseForwardAction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(InputAction), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo3 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).ToggleLauncher,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).ToggleLauncher = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ToggleLauncher",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("ToggleLauncher", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].IsGetNullable = false;
		array[2].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo4 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).HideLauncher,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).HideLauncher = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HideLauncher",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("HideLauncher", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].IsGetNullable = false;
		array[3].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo5 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).NextPage,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).NextPage = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "NextPage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("NextPage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].IsGetNullable = false;
		array[4].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo6 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).PreviousPage,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).PreviousPage = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PreviousPage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("PreviousPage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].IsGetNullable = false;
		array[5].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo7 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).ToggleDesktopIcons,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).ToggleDesktopIcons = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ToggleDesktopIcons",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("ToggleDesktopIcons", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		array[6].IsGetNullable = false;
		array[6].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo8 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).ToggleDockRevealLock,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).ToggleDockRevealLock = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ToggleDockRevealLock",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("ToggleDockRevealLock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		array[7].IsGetNullable = false;
		array[7].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo9 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).ToggleGameSleep,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).ToggleGameSleep = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ToggleGameSleep",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("ToggleGameSleep", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		array[8].IsGetNullable = false;
		array[8].IsSetNullable = false;
		JsonPropertyInfoValues<bool> propertyInfo10 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).HotCornersEnabled,
			Setter = delegate(object obj, bool value)
			{
				((ShortcutSettings)obj).HotCornersEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HotCornersEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("HotCornersEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<HotCorner> propertyInfo11 = new JsonPropertyInfoValues<HotCorner>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).HotCornerTarget,
			Setter = delegate(object obj, HotCorner value)
			{
				((ShortcutSettings)obj).HotCornerTarget = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HotCornerTarget",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("HotCornerTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotCorner), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<int> propertyInfo12 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).HotCornerDwellMs,
			Setter = delegate(object obj, int value)
			{
				((ShortcutSettings)obj).HotCornerDwellMs = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "HotCornerDwellMs",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("HotCornerDwellMs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<bool> propertyInfo13 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).MouseSideButtonsEnabled,
			Setter = delegate(object obj, bool value)
			{
				((ShortcutSettings)obj).MouseSideButtonsEnabled = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MouseSideButtonsEnabled",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("MouseSideButtonsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo14 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).MouseBackButton,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).MouseBackButton = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MouseBackButton",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("MouseBackButton", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		array[13].IsGetNullable = false;
		array[13].IsSetNullable = false;
		JsonPropertyInfoValues<HotkeyDefinition> propertyInfo15 = new JsonPropertyInfoValues<HotkeyDefinition>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ShortcutSettings),
			Converter = null,
			Getter = (object obj) => ((ShortcutSettings)obj).MouseForwardButton,
			Setter = delegate(object obj, HotkeyDefinition? value)
			{
				((ShortcutSettings)obj).MouseForwardButton = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MouseForwardButton",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ShortcutSettings).GetProperty("MouseForwardButton", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(HotkeyDefinition), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		array[14].IsGetNullable = false;
		array[14].IsSetNullable = false;
		return array;
	}

	private void ShortcutSettingsSerializeHandler(Utf8JsonWriter writer, ShortcutSettings? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WritePropertyName(PropName_mouseBackAction);
		JsonSerializer.Serialize(writer, value.MouseBackAction, InputAction);
		writer.WritePropertyName(PropName_mouseForwardAction);
		JsonSerializer.Serialize(writer, value.MouseForwardAction, InputAction);
		HotkeyDefinition toggleLauncher = value.ToggleLauncher;
		if (toggleLauncher != null)
		{
			writer.WritePropertyName(PropName_toggleLauncher);
			HotkeyDefinitionSerializeHandler(writer, toggleLauncher);
		}
		HotkeyDefinition hideLauncher = value.HideLauncher;
		if (hideLauncher != null)
		{
			writer.WritePropertyName(PropName_hideLauncher);
			HotkeyDefinitionSerializeHandler(writer, hideLauncher);
		}
		HotkeyDefinition nextPage = value.NextPage;
		if (nextPage != null)
		{
			writer.WritePropertyName(PropName_nextPage);
			HotkeyDefinitionSerializeHandler(writer, nextPage);
		}
		HotkeyDefinition previousPage = value.PreviousPage;
		if (previousPage != null)
		{
			writer.WritePropertyName(PropName_previousPage);
			HotkeyDefinitionSerializeHandler(writer, previousPage);
		}
		HotkeyDefinition toggleDesktopIcons = value.ToggleDesktopIcons;
		if (toggleDesktopIcons != null)
		{
			writer.WritePropertyName(PropName_toggleDesktopIcons);
			HotkeyDefinitionSerializeHandler(writer, toggleDesktopIcons);
		}
		HotkeyDefinition toggleDockRevealLock = value.ToggleDockRevealLock;
		if (toggleDockRevealLock != null)
		{
			writer.WritePropertyName(PropName_toggleDockRevealLock);
			HotkeyDefinitionSerializeHandler(writer, toggleDockRevealLock);
		}
		HotkeyDefinition toggleGameSleep = value.ToggleGameSleep;
		if (toggleGameSleep != null)
		{
			writer.WritePropertyName(PropName_toggleGameSleep);
			HotkeyDefinitionSerializeHandler(writer, toggleGameSleep);
		}
		writer.WriteBoolean(PropName_hotCornersEnabled, value.HotCornersEnabled);
		writer.WritePropertyName(PropName_hotCornerTarget);
		JsonSerializer.Serialize(writer, value.HotCornerTarget, HotCorner);
		writer.WriteNumber(PropName_hotCornerDwellMs, value.HotCornerDwellMs);
		writer.WriteBoolean(PropName_mouseSideButtonsEnabled, value.MouseSideButtonsEnabled);
		HotkeyDefinition mouseBackButton = value.MouseBackButton;
		if (mouseBackButton != null)
		{
			writer.WritePropertyName(PropName_mouseBackButton);
			HotkeyDefinitionSerializeHandler(writer, mouseBackButton);
		}
		HotkeyDefinition mouseForwardButton = value.MouseForwardButton;
		if (mouseForwardButton != null)
		{
			writer.WritePropertyName(PropName_mouseForwardButton);
			HotkeyDefinitionSerializeHandler(writer, mouseForwardButton);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<StatusBarEdge> Create_StatusBarEdge(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<StatusBarEdge> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(StatusBarEdge), new JsonStringEnumConverter<StatusBarEdge>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<StatusBarEdge>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ThemeMode> Create_ThemeMode(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ThemeMode> jsonTypeInfo))
		{
			JsonConverter converter = ExpandConverter(typeof(ThemeMode), new JsonStringEnumConverter<ThemeMode>(), options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ThemeMode>(options, converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<List<AppEntry>> Create_ListAppEntry(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<AppEntry>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<AppEntry>> collectionInfo = new JsonCollectionInfoValues<List<AppEntry>>
			{
				ObjectCreator = () => new List<AppEntry>(),
				SerializeHandler = ListAppEntrySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<AppEntry>, AppEntry>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListAppEntrySerializeHandler(Utf8JsonWriter writer, List<AppEntry>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			AppEntrySerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<LauncherGroup>> Create_ListLauncherGroup(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<LauncherGroup>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<LauncherGroup>> collectionInfo = new JsonCollectionInfoValues<List<LauncherGroup>>
			{
				ObjectCreator = () => new List<LauncherGroup>(),
				SerializeHandler = ListLauncherGroupSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<LauncherGroup>, LauncherGroup>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListLauncherGroupSerializeHandler(Utf8JsonWriter writer, List<LauncherGroup>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			LauncherGroupSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<LauncherPage>> Create_ListLauncherPage(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<LauncherPage>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<LauncherPage>> collectionInfo = new JsonCollectionInfoValues<List<LauncherPage>>
			{
				ObjectCreator = () => new List<LauncherPage>(),
				SerializeHandler = ListLauncherPageSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<LauncherPage>, LauncherPage>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListLauncherPageSerializeHandler(Utf8JsonWriter writer, List<LauncherPage>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			LauncherPageSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<LauncherSlot>> Create_ListLauncherSlot(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<LauncherSlot>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<LauncherSlot>> collectionInfo = new JsonCollectionInfoValues<List<LauncherSlot>>
			{
				ObjectCreator = () => new List<LauncherSlot>(),
				SerializeHandler = ListLauncherSlotSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<LauncherSlot>, LauncherSlot>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListLauncherSlotSerializeHandler(Utf8JsonWriter writer, List<LauncherSlot>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			LauncherSlotSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<string>> Create_ListString(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<string>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<string>> collectionInfo = new JsonCollectionInfoValues<List<string>>
			{
				ObjectCreator = () => new List<string>(),
				SerializeHandler = ListStringSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<string>, string>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListStringSerializeHandler(Utf8JsonWriter writer, List<string>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			writer.WriteStringValue(value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<DateTimeOffset> Create_DateTimeOffset(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DateTimeOffset> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DateTimeOffset>(options, JsonMetadataServices.DateTimeOffsetConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<DateTimeOffset?> Create_NullableDateTimeOffset(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DateTimeOffset?> jsonTypeInfo))
		{
			JsonConverter nullableConverter = JsonMetadataServices.GetNullableConverter<DateTimeOffset>(options);
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DateTimeOffset?>(options, nullableConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<int> Create_Int32(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<int> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<string> Create_String(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<string> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	public GlassDeskJsonContext()
		: base(null)
	{
	}

	public GlassDeskJsonContext(JsonSerializerOptions options)
		: base(options)
	{
	}

	private static bool TryGetTypeInfoForRuntimeCustomConverter<TJsonMetadataType>(JsonSerializerOptions options, out JsonTypeInfo<TJsonMetadataType> jsonTypeInfo)
	{
		JsonConverter runtimeConverterForType = GetRuntimeConverterForType(typeof(TJsonMetadataType), options);
		if (runtimeConverterForType != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<TJsonMetadataType>(options, runtimeConverterForType);
			return true;
		}
		jsonTypeInfo = null;
		return false;
	}

	private static JsonConverter? GetRuntimeConverterForType(Type type, JsonSerializerOptions options)
	{
		for (int i = 0; i < options.Converters.Count; i++)
		{
			JsonConverter jsonConverter = options.Converters[i];
			if (jsonConverter != null && jsonConverter.CanConvert(type))
			{
				return ExpandConverter(type, jsonConverter, options, validateCanConvert: false);
			}
		}
		return null;
	}

	private static JsonConverter ExpandConverter(Type type, JsonConverter converter, JsonSerializerOptions options, bool validateCanConvert = true)
	{
		if (validateCanConvert && !converter.CanConvert(type))
		{
			throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
		}
		if (converter is JsonConverterFactory jsonConverterFactory)
		{
			converter = jsonConverterFactory.CreateConverter(type, options);
			if (converter == null || converter is JsonConverterFactory)
			{
				throw new InvalidOperationException($"The converter '{jsonConverterFactory.GetType()}' cannot return null or a JsonConverterFactory instance.");
			}
		}
		return converter;
	}

	public override JsonTypeInfo? GetTypeInfo(Type type)
	{
		base.Options.TryGetTypeInfo(type, out JsonTypeInfo typeInfo);
		return typeInfo;
	}

	JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == typeof(bool))
		{
			return Create_Boolean(options);
		}
		if (type == typeof(bool?))
		{
			return Create_NullableBoolean(options);
		}
		if (type == typeof(double))
		{
			return Create_Double(options);
		}
		if (type == typeof(InputAction))
		{
			return Create_InputAction(options);
		}
		if (type == typeof(AppEntry))
		{
			return Create_AppEntry(options);
		}
		if (type == typeof(AppItemKind))
		{
			return Create_AppItemKind(options);
		}
		if (type == typeof(AppLibraryData))
		{
			return Create_AppLibraryData(options);
		}
		if (type == typeof(BackupManifest))
		{
			return Create_BackupManifest(options);
		}
		if (type == typeof(DockLayout))
		{
			return Create_DockLayout(options);
		}
		if (type == typeof(GlassMaterialMode))
		{
			return Create_GlassMaterialMode(options);
		}
		if (type == typeof(LauncherGroup))
		{
			return Create_LauncherGroup(options);
		}
		if (type == typeof(LauncherLayout))
		{
			return Create_LauncherLayout(options);
		}
		if (type == typeof(LauncherPage))
		{
			return Create_LauncherPage(options);
		}
		if (type == typeof(LauncherSlot))
		{
			return Create_LauncherSlot(options);
		}
		if (type == typeof(LauncherSlotKind))
		{
			return Create_LauncherSlotKind(options);
		}
		if (type == typeof(AppearanceSettings))
		{
			return Create_AppearanceSettings(options);
		}
		if (type == typeof(DesktopTaskbarSettings))
		{
			return Create_DesktopTaskbarSettings(options);
		}
		if (type == typeof(DisplaySettings))
		{
			return Create_DisplaySettings(options);
		}
		if (type == typeof(DockPlacement))
		{
			return Create_DockPlacement(options);
		}
		if (type == typeof(DockSettings))
		{
			return Create_DockSettings(options);
		}
		if (type == typeof(GlassDeskSettings))
		{
			return Create_GlassDeskSettings(options);
		}
		if (type == typeof(HotCorner))
		{
			return Create_HotCorner(options);
		}
		if (type == typeof(HotkeyDefinition))
		{
			return Create_HotkeyDefinition(options);
		}
		if (type == typeof(LauncherSettings))
		{
			return Create_LauncherSettings(options);
		}
		if (type == typeof(PerformanceSettings))
		{
			return Create_PerformanceSettings(options);
		}
		if (type == typeof(ShortcutSettings))
		{
			return Create_ShortcutSettings(options);
		}
		if (type == typeof(StatusBarEdge))
		{
			return Create_StatusBarEdge(options);
		}
		if (type == typeof(ThemeMode))
		{
			return Create_ThemeMode(options);
		}
		if (type == typeof(List<AppEntry>))
		{
			return Create_ListAppEntry(options);
		}
		if (type == typeof(List<LauncherGroup>))
		{
			return Create_ListLauncherGroup(options);
		}
		if (type == typeof(List<LauncherPage>))
		{
			return Create_ListLauncherPage(options);
		}
		if (type == typeof(List<LauncherSlot>))
		{
			return Create_ListLauncherSlot(options);
		}
		if (type == typeof(List<string>))
		{
			return Create_ListString(options);
		}
		if (type == typeof(DateTimeOffset))
		{
			return Create_DateTimeOffset(options);
		}
		if (type == typeof(DateTimeOffset?))
		{
			return Create_NullableDateTimeOffset(options);
		}
		if (type == typeof(int))
		{
			return Create_Int32(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		return null;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
