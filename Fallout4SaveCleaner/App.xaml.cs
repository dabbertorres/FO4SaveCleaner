using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;

namespace Fallout4SaveCleaner
{
	public partial class App : Application
	{
		private const string FALLOUT_4_SAVE_DIR = "My Games/Fallout4/Saves";
		private const string FALLOUT_4_SAVE_BACKUP_DIR = "My Games/Fallout4/Backups";

		public readonly string userDocumentsPath;
		public readonly string userFo4SaveDir;
		public readonly string userFo4BackupsDir;

		// don't want others screwing with all my hard work
		private List<Save> saves;

		// ...but I do want them to see it because I want to show off
		public readonly ReadOnlyCollection<Save> saveList;

		public App()
		{
			userDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			userFo4SaveDir = Path.Combine(userDocumentsPath, FALLOUT_4_SAVE_DIR);
			userFo4BackupsDir = Path.Combine(userDocumentsPath, FALLOUT_4_SAVE_BACKUP_DIR);

			saves = GetSaves(userFo4SaveDir);
			saveList = saves.AsReadOnly();
		}

		private static List<Save> GetSaves(string path)
		{
			List<string> saveFiles = new List<string>(Directory.GetFiles(path, "*.fos"));

			object saveListLock = new object();

			List<Save> saves = new List<Save>();

			// counts down to 0, storing how many jobs we're waiting on
			int countdown = saveFiles.Count;

			// queue each file up for parsing
			saveFiles.ForEach((file) => ThreadPool.QueueUserWorkItem((o) => MakeSave(file, saves, saveListLock, ref countdown)));

			// block until done
			while(countdown != 0);

			return saves;
		}

		private static void MakeSave(string file, List<Save> saves, object savesLock, ref int countdown)
		{
			Save s = new Save(file);

			lock (savesLock)
			{
				saves.Add(s);
			}

			--countdown;
		}
	}
}
