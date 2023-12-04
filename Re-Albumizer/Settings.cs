using System;
using System.IO;

using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

namespace Re_Albumizer; 

internal static class Settings
{
	internal static RegistryKey SettingsKey = null!;

	public static void Init()
	{
		SettingsKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true)?.CreateSubKey("Re-Albumizer") ?? throw new IOException("Cannot Write to Software Settings");
		if (SettingsKey.GetValue("ShowSaveDialog") == null)
		{
			SettingsKey.SetValue("ShowSaveDialog", 1, RegistryValueKind.DWord);
		}

		if (SettingsKey.GetValue("Backup") == null)
		{
			SettingsKey.SetValue("Backup", 0, RegistryValueKind.DWord);
		}
	}
}