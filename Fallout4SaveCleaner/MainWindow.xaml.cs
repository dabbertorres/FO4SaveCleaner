using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System;

namespace Fallout4SaveCleaner
{
	public partial class MainWindow : Window
	{
		private App app;

		public MainWindow()
		{
			InitializeComponent();
			app = (App)Application.Current;

			Closing += Exiting;
		}

		private void CleanClicked(object sender, RoutedEventArgs e)
		{
			// we busy!
			cleanButton.Content = "Cleaning...";
			cleanButton.IsEnabled = false;

			// keep UI thread clear
			ThreadPool.QueueUserWorkItem((o) =>
			{
				var toDelete = app.saveList.Where(save => save.type == Save.Type.Normal).   // ignore Auto and Quick saves
											GroupBy(save => save.character).                // group by each character
											SelectMany(character => character.
											OrderByDescending(save => save.playTime).       // sort by playtime (highest first)
											Skip(SavesToKeep()));                           // and skip the user's chosen amount to preserve, taking the rest

				// default (no backups worker function)
				Action<string> work = (file) => File.Delete(Path.Combine(app.userFo4SaveDir, file));

				// but, if the user wants backups, move the saves, rather than just delete
				if(MakeBackups())
				{
					Directory.CreateDirectory(app.userFo4BackupsDir);	// make sure our backup dir exists
					work = (file) => File.Move(Path.Combine(app.userFo4SaveDir, file), Path.Combine(app.userFo4BackupsDir, file));
				}

				foreach(var save in toDelete)
				{
					work(save.filename);
				}

				// signal done
				cleanButton.Dispatcher.Invoke(() =>
				{
					cleanButton.Content = "Clean";
					cleanButton.IsEnabled = true;
					MessageBox.Show("Done!");
				});
			});
		}

		// ease of use function
		private int SavesToKeep()
		{
			Func<int> get = () => int.Parse(savesToKeepInput.Text);

			if(Dispatcher.CheckAccess())
				return get();
			else
				return Dispatcher.Invoke(get);
		}

		private bool MakeBackups()
		{
			Func<bool> get = () => makeBackupsCheck.IsChecked ?? false;

			if(Dispatcher.CheckAccess())
				return get();
			else
				return Dispatcher.Invoke(get);
		}

		// get a chance to save our settings
		private void Exiting(object sender, CancelEventArgs e)
		{
			var settings = Properties.Settings.Default;
			settings.NumberToPreserve = SavesToKeep();
			settings.MakeBackups = MakeBackups();
		}

		// events from the TextBox to make sure we can only get positive numbers...
		private void ValidateSavesToKeepNumberInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = IsValidText(e.Text);
		}

		private void ValidateSavesToKeepNumberPaste(object sender, DataObjectPastingEventArgs e)
		{
			if(!IsValidText(e.DataObject.GetData(typeof(string)) as string))
				e.CancelCommand();
		}

		// only positive values are valid
		private static bool IsValidText(string text)
		{
			// yeah
			uint argumentSatisfierVariableBecauseThereIsNoBuiltInIsUintFunctionAndIAmLazy;
			return uint.TryParse(text, out argumentSatisfierVariableBecauseThereIsNoBuiltInIsUintFunctionAndIAmLazy);
		}
	}
}
