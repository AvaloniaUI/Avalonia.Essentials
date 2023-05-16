using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;

namespace Samples.ViewModel
{
	public class FileSystemViewModel : BaseViewModel
	{
		const string templateFileName = "FileSystemTemplate.txt";
		const string localFileName = "TheFile.txt";

		static string localPath = Path.Combine(FileSystem.AppDataDirectory, localFileName);

		string currentContents;

		public FileSystemViewModel()
		{
			LoadFileCommand = new RelayCommand(DoLoadFile);
			SaveFileCommand = new RelayCommand(DoSaveFile);
			DeleteFileCommand = new RelayCommand(DoDeleteFile);

			DoLoadFile();
		}

		public ICommand LoadFileCommand { get; }

		public ICommand SaveFileCommand { get; }

		public ICommand DeleteFileCommand { get; }

		public string AppDataDirectory => FileSystem.AppDataDirectory;

		public string CacheDirectory => FileSystem.CacheDirectory;

		public string CurrentContents
		{
			get => currentContents;
			set => SetProperty(ref currentContents, value);
		}

		async void DoLoadFile()
		{
			if (File.Exists(localPath))
			{
				CurrentContents = File.ReadAllText(localPath);
			}
			else
			{
				using (var stream = await FileSystem.OpenAppPackageFileAsync(templateFileName))
				using (var reader = new StreamReader(stream))
				{
					CurrentContents = await reader.ReadToEndAsync();
				}
			}
		}

		void DoSaveFile()
		{
			var dir = Path.GetDirectoryName(localPath);
			Directory.CreateDirectory(dir);

			File.WriteAllText(localPath, CurrentContents);
		}

		void DoDeleteFile()
		{
			if (File.Exists(localPath))
				File.Delete(localPath);
		}
	}
}
