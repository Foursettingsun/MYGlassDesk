using System;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;

namespace GlassDesk.Core.Storage;

public sealed class GlassDeskDataStore
{
	public AppPaths Paths { get; }

	public SettingsStore Settings { get; }

	public AppLibraryStore Library { get; }

	public LayoutStore Layouts { get; }

	public BackupService Backup { get; }

	public GlassDeskDataStore(AppPaths paths, TimeProvider? timeProvider = null)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		Paths = paths;
		Settings = new SettingsStore(paths.SettingsFile, timeProvider);
		Library = new AppLibraryStore(paths.AppLibraryFile, timeProvider);
		Layouts = new LayoutStore(paths.DockLayoutFile, paths.LauncherLayoutFile, timeProvider);
		Backup = new BackupService(paths, timeProvider);
	}

	public DataLoadReport LoadAll()
	{
		Paths.EnsureCreated();
		DataLoadReport dataLoadReport = new DataLoadReport();
		LoadOutcome<GlassDeskSettings> loadOutcome = Settings.Load();
		dataLoadReport.SettingsStatus = loadOutcome.Status;
		Describe(dataLoadReport, loadOutcome, "设置");
		LoadOutcome<AppLibraryData> loadOutcome2 = Library.Load();
		dataLoadReport.LibraryStatus = loadOutcome2.Status;
		Describe(dataLoadReport, loadOutcome2, "应用库");
		Describe(dataLoadReport, Layouts.LoadDock(), "Dock 布局");
		Describe(dataLoadReport, Layouts.LoadLauncher(), "启动台布局");
		if (Library.AnomalousEntryCount > 0)
		{
			dataLoadReport.Notices.Add($"应用库中有 {Library.AnomalousEntryCount} 条记录无法使用（可能是空条目或缺字段），已跳过；记录仍保留在文件中。");
		}
		if (Library.RepairedIconReferenceCount > 0)
		{
			dataLoadReport.Notices.Add($"有 {Library.RepairedIconReferenceCount} 条记录的自定义图标引用指向旧的数据目录，已改回当前目录。");
		}
		return dataLoadReport;
	}

	public bool DeleteFromLibrary(string? itemId)
	{
		if (!Library.Remove(itemId))
		{
			return false;
		}
		Layouts.PurgeItem(itemId);
		Library.Save();
		Layouts.SaveDock();
		Layouts.SaveLauncher();
		return true;
	}

	private static void Describe<T>(DataLoadReport report, LoadOutcome<T> outcome, string label) where T : class
	{
		if (outcome.PreservedFile != null)
		{
			report.PreservedFiles.Add(outcome.PreservedFile);
		}
		switch (outcome.Status)
		{
		case LoadStatus.RecoveredFromBackup:
			report.Notices.Add(label + "文件损坏，已从备份恢复。");
			break;
		case LoadStatus.Unrecoverable:
			report.Notices.Add(label + "文件与其备份均无法读取，已使用默认值。");
			break;
		default:
			report.Notices.Add(label + "文件状态未知。");
			break;
		case LoadStatus.Loaded:
		case LoadStatus.FirstRun:
			break;
		}
		if (outcome.PreservedFile != null)
		{
			report.Notices.Add("损坏的" + label + "文件已保留，便于排查。");
		}
	}
}
