using System;
using System.Text.Json;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Serialization;

namespace GlassDesk.Core.Storage;

public sealed class SettingsStore
{
	private readonly JsonFileStore<GlassDeskSettings> _store;

	public GlassDeskSettings Current { get; private set; } = new GlassDeskSettings();

	public string FilePath => _store.Path;

	public SettingsStore(string path, TimeProvider? timeProvider = null)
	{
		_store = new JsonFileStore<GlassDeskSettings>(path, GlassDeskJsonContext.Default.GlassDeskSettings, timeProvider);
	}

	public LoadOutcome<GlassDeskSettings> Load()
	{
		LoadOutcome<GlassDeskSettings> loadOutcome = _store.Load(() => new GlassDeskSettings());
		GlassDeskSettings value = loadOutcome.Value;
		GlassDeskSettings glassDeskSettings = value;
		bool? onboardingDismissed = glassDeskSettings.OnboardingDismissed;
		bool valueOrDefault = onboardingDismissed == true;
		if (!onboardingDismissed.HasValue)
		{
			valueOrDefault = loadOutcome.Status != LoadStatus.FirstRun;
			bool? onboardingDismissed2 = valueOrDefault;
			glassDeskSettings.OnboardingDismissed = onboardingDismissed2;
		}
		value.Normalize();
		Migrate(value, (loadOutcome.Status == LoadStatus.Loaded) ? ReadSchemaVersionOnDisk() : value.SchemaVersion);
		Current = value;
		return loadOutcome;
	}

	public void Save()
	{
		Current.Normalize();
		_store.Save(Current);
	}

	public void DismissOnboarding()
	{
		bool? onboardingDismissed = Current.OnboardingDismissed;
		Current.OnboardingDismissed = true;
		try
		{
			Save();
		}
		catch (StorageException)
		{
			Current.OnboardingDismissed = onboardingDismissed;
			throw;
		}
	}

	private int ReadSchemaVersionOnDisk()
	{
		string text = AtomicFile.ReadAllTextOrNull(_store.Path);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 1;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(text);
			if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object && jsonDocument.RootElement.TryGetProperty("schemaVersion", out var value) && value.TryGetInt32(out var value2))
			{
				return value2;
			}
		}
		catch (JsonException)
		{
		}
		return 1;
	}

	private static void Migrate(GlassDeskSettings settings, int fromVersion)
	{
		_ = 1;
		settings.SchemaVersion = 1;
	}
}
