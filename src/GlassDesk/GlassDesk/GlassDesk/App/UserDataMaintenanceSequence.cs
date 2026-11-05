using System;
using GlassDesk.AppServices;

namespace GlassDesk.App;

internal static class UserDataMaintenanceSequence
{
	public static UserDataDeletionResult? Execute(Action closeCommandDispatch, Action prepareSettings, Action stopInteractions, Action stopTray, Action stopServices, Action closeMessageWindow, Action releaseDataLease, Func<UserDataDeletionResult> deleteData, Action<Exception> reportStopFailure, Action<UserDataDeletionResult> presentResult, Action shutdown)
	{
		ArgumentNullException.ThrowIfNull(closeCommandDispatch, "closeCommandDispatch");
		ArgumentNullException.ThrowIfNull(prepareSettings, "prepareSettings");
		ArgumentNullException.ThrowIfNull(stopInteractions, "stopInteractions");
		ArgumentNullException.ThrowIfNull(stopTray, "stopTray");
		ArgumentNullException.ThrowIfNull(stopServices, "stopServices");
		ArgumentNullException.ThrowIfNull(closeMessageWindow, "closeMessageWindow");
		ArgumentNullException.ThrowIfNull(releaseDataLease, "releaseDataLease");
		ArgumentNullException.ThrowIfNull(deleteData, "deleteData");
		ArgumentNullException.ThrowIfNull(reportStopFailure, "reportStopFailure");
		ArgumentNullException.ThrowIfNull(presentResult, "presentResult");
		ArgumentNullException.ThrowIfNull(shutdown, "shutdown");
		try
		{
			closeCommandDispatch();
			prepareSettings();
			stopInteractions();
			stopTray();
			stopServices();
			closeMessageWindow();
			releaseDataLease();
		}
		catch (Exception obj)
		{
			reportStopFailure(obj);
			shutdown();
			return null;
		}
		UserDataDeletionResult userDataDeletionResult = deleteData();
		presentResult(userDataDeletionResult);
		shutdown();
		return userDataDeletionResult;
	}
}
