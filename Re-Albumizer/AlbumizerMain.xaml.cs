using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using TagLib;
using static System.Windows.Forms.DialogResult;
using File = TagLib.File;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;
using TaskDialog = Ookii.Dialogs.Wpf.TaskDialog;
using TaskDialogButton = Ookii.Dialogs.Wpf.TaskDialogButton;
using TaskDialogIcon = Ookii.Dialogs.Wpf.TaskDialogIcon;
using TextBox = System.Windows.Controls.TextBox;

namespace Re_Albumizer;

/// <summary>
///     A Structure for storing a Song
/// </summary>
public class MusicFile
{
    public MusicFile(
        uint track,
        string title,
        string performers,
        string album,
        string path,
        File file
    )
    {
        TrackId = track;
        Title = title;
        Performers = performers;
        Album = album;
        fileLoc = path;
        TaglibFile = file;
    }

    public uint TrackId { get; set; }
    public string Title { get; set; }
    public string Performers { get; set; }
    public string Album { get; set; }
    public string fileLoc { get; set; }
    public File TaglibFile { get; set; }
}

public enum TextEditPosition : byte
{
    Title,
    Performers,
    Track,
    Album
}

/// <summary>
///     Interaction logic for AlbumizerMain.xaml
/// </summary>
public partial class AlbumizerMain : Window
{
    private void CheckUnsavedChanges(object? sender, CancelEventArgs e)
    {
        if (UnsavedWork)
        {
            if (((int?)Settings.SettingsKey.GetValue("ShowSaveDialog") ?? 1) == 1)
            {
                TaskDialogButton saveButton = new TaskDialogButton("Save");
                TaskDialogButton discardButton = new TaskDialogButton("Discard");
                TaskDialogButton cancelButton = new TaskDialogButton("Cancel");
                cancelButton.Default = true;
                cancelButton.ButtonType = ButtonType.Cancel;
                TaskDialog uworkDialog = new TaskDialog
                {
                    MainIcon = TaskDialogIcon.Information,
                    MainInstruction = "You Have Unsaved Changes!",

                    Content = "Are you sure you want to discard all changes?",
                    Buttons = { saveButton, discardButton, cancelButton },
                    VerificationText = "Dont Ask Again"
                };


                switch (uworkDialog.ShowDialog())
                {
                    case var value when value == saveButton:
                        this.SaveVal(null, null);
                        if (uworkDialog.IsVerificationChecked)
                        {
                            Settings.SettingsKey.SetValue("SaveDialogDefaultButton", "Save", RegistryValueKind.String);
                            Settings.SettingsKey.SetValue("ShowSaveDialog", 0, RegistryValueKind.DWord);
                        }


                        break;
                    case var value when value == discardButton:
                        if (uworkDialog.IsVerificationChecked)
                        {
                            Settings.SettingsKey.SetValue("SaveDialogDefaultButton", "Discard",
                                RegistryValueKind.String);
                            Settings.SettingsKey.SetValue("ShowSaveDialog", 0, RegistryValueKind.DWord);
                        }

                        break;
                    case var value when value == cancelButton || value == null:
                        e.Cancel = true;
                        break;
                }
            }
            else
            {
                switch (Settings.SettingsKey.GetValue("SaveDialogDefaultButton"))
                {
                    case "Save":
                        this.SaveVal(null, null);
                        break;
                    case "Discard":
                        break;
                    case null:
                        this.SaveVal(null, null);
                        Settings.SettingsKey.SetValue("SaveDialogDefaultButton", "Save", RegistryValueKind.String);
                        break;
                }
            }
        }
    }

#region Album Control Setters

    //Handles Changing album art
    private void OnChangeAlbumArt(object sender, RoutedEventArgs e)
    {
        VistaOpenFileDialog selNewFile = new VistaOpenFileDialog
        {
            Multiselect = false,
            Title = Properties.Resources.SelNewAlbumArt,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        if (selNewFile.ShowDialog() == false)
            return;
        foreach (MusicFile song in SongList)
        {
            AlbumArt.Source = this.FromHBitmap(new Bitmap(selNewFile.FileName));
            song.TaglibFile.Tag.Pictures = new IPicture[] { new Picture(selNewFile.FileName) };
        }
    }
/*
    private void AAlbumNameChange(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Album Name"
        };
        string res = edobj.ShowForm();
        if (res != "")
            foreach (MusicFile song in SongList)
            {
                song.TaglibFile.Tag.Album = res;
                song.Album = res;
                ACtrlAlbumName.Text = $"Album: {res}";
            }
    }

    private void AYearChange(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter Album Year"
        };
        string res = edobj.ShowForm();
        //MessageBox.Show(int.Parse(res).ToString());
        if (res != "")
            foreach (MusicFile song in SongList)
            {
                if (!UInt32.TryParse(res, NumberStyles.Integer, null, out uint year))
                    if (
                        MessageBox.Show(
                            Properties.Resources.YearChangeYearMalformatText,
                            Properties.Resources.YearChangeYearMalformatCaption,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        ) == OK
                    )
                        return;

                song.TaglibFile.Tag.Year = year;
                ACtrlYear.Text = $"Year: {res}";
            }
    }

    private void AArtistChange(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Artists Name"
        };
        string[] res = edobj.ShowForm();
        //MessageBox.Show(res);
        if (res.Length != 0)
            foreach (MusicFile song in SongList)
            {
                song.TaglibFile.Tag.AlbumArtists = res;
                ACtrlMainArtist.Text = $"Album Artists: {String.Join(",", res)}";
            }
    }

    private void AGenreChange(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Genre"
        };
        string[] res = edobj.ShowForm();
        if (res.Length != 0)
            foreach (MusicFile song in SongList)
            {
                song.TaglibFile.Tag.Genres = res;
                ACtrlGenre.Text = $"Genres: {String.Join(",", res)}";
            }
    }*/

#endregion Album Control Setters

#region Song Control Handlers

    //TODO:change to save
    /*
    private void OnSCtrlTitle(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Song Title"
        };
        string res = edobj.ShowForm();
        if (res != "")
        {
            MusicFile song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Title = res;
            song.Title = res;
            SCtrlTitle.Text = $"Title: {res}";

            //Refresh ListView
            ICollectionView? view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }

    private void OnSCtrlTrackNo(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Track Number"
        };
        string res = edobj.ShowForm();
        if (res != "")
        {
            if (!UInt32.TryParse(res, NumberStyles.Integer, null, out uint Track))
                if (
                    MessageBox.Show(
                        Properties.Resources.InvalidTrackNo,
                        Properties.Resources.YearChangeYearMalformatCaption,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    ) == OK
                )
                    return;

            MusicFile song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Track = Track;
            song.TrackId = Track;
            SCtrlTrackNo.Text = $"Track No.: {res}";

            ICollectionView? view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }

    private void OnSCtrlContribArt(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Artists Name"
        };
        string[] res = edobj.ShowForm();
        //MessageBox.Show(res);
        if (res.Length != 0)
        {
            MusicFile song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Performers = res;
            song.Performers = String.Join(",", res);
            SCtrlContribArt.Text = $"Artists: {String.Join(",", res)}";

            ICollectionView? view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }

    private void OnSCtrlComposer(object sender, RoutedEventArgs e)
    {
        EditObject edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Composer Name"
        };
        string[] res = edobj.ShowForm();
        //MessageBox.Show(res);
        if (res.Length != 0)
        {
            MusicFile song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Composers = res;
            SCtrlComposer.Text = $"Composers: {String.Join(",", res)}";

            ICollectionView? view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }*/

    private void OnSongSelectChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (SongList.Count != 0)
            {
                SCtrlTitle.Text = SongList[SongListElement.SelectedIndex].Title;
                SCtrlTrackNo.Text =
                    SongList[SongListElement.SelectedIndex].TrackId.ToString();
                SCtrlContribArt.Text =
                    String.Join("; ", SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Performers);
                SCtrlComposer.Text =
                    String.Join("; ", SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Composers);
            }
        }
        catch (Exception)
        {
            // This Fires If we Nuke the song list, as this is not a error, we simply ignore and set the selection to "nothing"
            SongListElement.SelectedIndex = 0;
        }
    }

#endregion Song Control Handlers


#region Define

    private bool UnsavedWork;
    private string? _path;
    public ObservableCollection<MusicFile> SongList = new ObservableCollection<MusicFile>();

#endregion Define

#region Album Makers

    public AlbumizerMain()
    {
        Settings.Init();

        this.InitializeComponent();
        //evil WPF hacks
        SongListElement.ItemsSource = SongList;
    }

    private void NewAlbumFolder(object sender, RoutedEventArgs e)
    {
        AlbumArt.Source = this.FromHBitmap(new Bitmap(Properties.Resources.Nullart));
        //SongList.Clear();
        EditObject edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Album Folder Name"
        };
        string res = edobj.ShowForm();
        if (res != "")
        {
            VistaFolderBrowserDialog albumFolderLoader = new VistaFolderBrowserDialog
            {
                UseDescriptionForTitle = true,
                Description = Properties.Resources.SelectNewFolderPath
            };
            if (albumFolderLoader.ShowDialog() == false)
                return;
            Directory.CreateDirectory(albumFolderLoader.SelectedPath + @"\" + res);
            _path = albumFolderLoader.SelectedPath + @"\" + res;
            LoadedAlbum_Text.Content = "Current Album: " + _path ?? "None";
            EditMenu.IsEnabled = true;
            AlbumTab.IsEnabled = true;
            SongTab.IsEnabled = true;
            //fixes a dumb problem
            VistaOpenFileDialog selNewFile = new VistaOpenFileDialog
            {
                Multiselect = false,
                Title = Properties.Resources.selectnewsongPrompt,
                Filter = "*.mp3|*.MP3|*.m4a|*.M4A",
                InitialDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile
                )
            };
            if (selNewFile.ShowDialog() == false)
                return;
            FileInfo fl = new FileInfo(selNewFile.FileName);
            fl.MoveTo(_path + @"\" + fl.Name);
            File? currMp3 = File.Create(_path + @"\" + fl.Name);
            if (currMp3.TagTypes == TagTypes.None)
                SongList.Add(
                    new MusicFile(
                        0,
                        Properties.Resources.NoSong,
                        Properties.Resources.NoSong,
                        Properties.Resources.NoSong,
                        _path + @"\" + fl.Name,
                        currMp3
                    )
                );
            else
                SongList.Add(
                    new MusicFile(
                        currMp3.Tag.Track,
                        currMp3.Tag.Title,
                        String.Join(",", currMp3.Tag.Performers),
                        currMp3.Tag.Album,
                        _path + @"\" + fl.Name,
                        currMp3
                    )
                );
        }
    }

    private void LoadNewAlbum(object sender, RoutedEventArgs e)
    {
        //this was supposed to disable the edit menu untill you load an album, but that was never implemented. why?
        EditMenu.IsEnabled = true;

        UnsavedWork = false;
        if (SongListElement.Items.Count == 0)
        {
            //TODO: Load files in correct order using song index
            //Load Folder
            VistaFolderBrowserDialog albumFolderLoader = new VistaFolderBrowserDialog
            {
                UseDescriptionForTitle = true,
                Description = Properties.Resources.FolderLoaderPrompt
            };
            if (albumFolderLoader.ShowDialog() == false)
                return;
            _path = albumFolderLoader.SelectedPath;

            LoadedAlbum_Text.Content =
                "Current Album: " + albumFolderLoader.SelectedPath ?? "None";
            MainAlbumControls.Visibility = Visibility.Visible;
            //Load Files

            //Directory.GetFiles(_path, "*.mp3")+(Directory.GetFiles(_path,".MP3"));
            string[] allowedExtentions = { ".mp3", ".m4a" };
            foreach (string file in Directory.GetFiles(_path).Where(file =>
                         allowedExtentions.Any(extention =>
                             file.Contains(extention, StringComparison.CurrentCultureIgnoreCase))).ToArray())
            {
                File? currMp3 = File.Create(file);

                SongList.Add(
                    new MusicFile(
                        currMp3.Tag.Track,
                        currMp3.Tag.Title,
                        String.Join(",", currMp3.Tag.Performers),
                        currMp3.Tag.Album,
                        file,
                        currMp3
                    )
                );
            }


            //check for no album art before blowing up the program
            //always use the album art of the first song (who has multiple arts for each song?)

            Bitmap? albumArtBitmap = null;
            if (SongList[0].TaglibFile.Tag.Pictures.Length == 0)
                albumArtBitmap = new Bitmap(Properties.Resources.Nullart);
            else
                albumArtBitmap = new Bitmap(
                    new MemoryStream(SongList[0].TaglibFile.Tag.Pictures[0].Data.Data)
                );

            AlbumArt.Source = this.FromHBitmap(albumArtBitmap);
            //force select to prevent modifying a null song
            SongListElement.SelectedIndex = 0;

            //you thought we were done here but No!
            ACtrlAlbumName.Text = $"{SongList[0].TaglibFile.Tag.Album}";
            ACtrlGenre.Text = $"{String.Join(",", SongList[0].TaglibFile.Tag.Genres)}";
            ACtrlMainArtist.Text =
                $"{String.Join(",", SongList[0].TaglibFile.Tag.AlbumArtists)}";
            ACtrlYear.Text = $"{SongList[0].TaglibFile.Tag.Year.ToString()}";
            AlbumTab.IsEnabled = true;
            SongTab.IsEnabled = true;
        }
        else
        {
            AlbumArt.Source = this.FromHBitmap(
                new Bitmap(Properties.Resources.Nullart)
            );
            SongList.Clear();
            //possible LOOP!!! (shouldn't occur)
            this.LoadNewAlbum(sender, e);
        }
    }

    private void AddSongToAlbum(object sender, RoutedEventArgs e)
    {
        VistaOpenFileDialog selNewFile = new VistaOpenFileDialog
        {
            Multiselect = false,
            Title = Properties.Resources.selectnewsongPrompt,
            Filter = "*.mp3|*MP3",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        if (selNewFile.ShowDialog() == false)
            return;
        FileInfo fl = new FileInfo(selNewFile.FileName);
        fl.CopyTo(_path + @"\" + fl.Name);
        File? currMp3 = File.Create(_path + @"\" + fl.Name);
        if (currMp3.TagTypes == TagTypes.None)
            SongList.Add(
                new MusicFile(
                    0,
                    Properties.Resources.NoSong,
                    Properties.Resources.NoSong,
                    Properties.Resources.NoSong,
                    _path + @"\" + fl.Name,
                    currMp3
                )
            );
        else
            SongList.Add(
                new MusicFile(
                    currMp3.Tag.Track,
                    currMp3.Tag.Title,
                    String.Join(",", currMp3.Tag.Performers),
                    SongList[0].Album,
                    _path + @"\" + fl.Name,
                    currMp3
                )
            );

        currMp3.Tag.Album = SongList[0].Album;
        currMp3.Tag.Pictures = SongList[0].TaglibFile.Tag.Pictures;
        currMp3.Tag.Genres = SongList[0].TaglibFile.Tag.Genres;
        currMp3.Tag.Year = SongList[0].TaglibFile.Tag.Year;
        currMp3.Tag.AlbumArtists = SongList[0].TaglibFile.Tag.AlbumArtists;
        //currMp3.Save();
        fl.Delete();
    }

    //Note:add later
    public void UpdateTrackCount()
    {
        foreach (MusicFile song in SongList) song.TaglibFile.Tag.TrackCount = (uint)SongList.Count;
    }

#endregion Album Makers

#region Auxillary Functions

    //Note:add Unsaved Changes Alert

    private void CheckFocusSubmit(object sender, RoutedEventArgs e)
    {
        this.Submit(sender);
    }

    //allows setting ImageSource from Bitmap
    public BitmapSource FromHBitmap(Bitmap bmp)
    {
        return Imaging.CreateBitmapSourceFromHBitmap(
            bmp.GetHbitmap(),
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions()
        );
    }

    public void ExitProg(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OpenAboutPage(object sender, RoutedEventArgs e)
    {
        //show about page

        throw new NotImplementedException("Screw You");
    }

    private void OpenAlbumFolder(object sender, RoutedEventArgs e)
    {
        if (_path != null)
            Process.Start("explorer.exe", _path);
    }

    private void RemoveItemFromAlbum(object sender, RoutedEventArgs e)
    {
        SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Album = "";
        SongList.RemoveAt(SongListElement.SelectedIndex);
    }

    private void DeleteSongFromDisk(object sender, RoutedEventArgs e)
    {
        if (
            MessageBox.Show(
                String.Format(
                    Properties.Resources.DeleteConfirmText,
                    SongList[SongListElement.SelectedIndex].Title
                ),
                Properties.Resources.DeleteConfirmCaption,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button2
            ) == Yes
        )
        {
            new FileInfo(SongList[SongListElement.SelectedIndex].fileLoc).Delete();
            SongList.RemoveAt(SongListElement.SelectedIndex);
        }
    }

#endregion Auxillary Functions

#region List Element Edit Handlers (And Global Save Function)

    private void SaveBackground(object? sender, DoWorkEventArgs e)
    {
        int trackTotalCount = SongList.Count - 1;
        int processed = 0;
        if (sender != null)
        {
            ProgressDialog self = (sender as ProgressDialog)!;
            foreach (MusicFile song in SongList)
            {
                int currentPercent =
                    (int)Math.Round((double)processed / trackTotalCount * 100, MidpointRounding.AwayFromZero);
                self.ReportProgress(currentPercent, null, $"Processing : \"{song.TaglibFile.Tag.Title}\"");

                song.TaglibFile.Tag.Performers = new[] { song.Performers };
                song.TaglibFile.Tag.Title = song.Title;
                song.TaglibFile.Tag.Album = song.Album;
                song.TaglibFile.Tag.Track = song.TrackId;
                if ((int)(Settings.SettingsKey.GetValue("Backup") ?? 0) == 1)
                {
                    DirectoryInfo backupsDir = Directory.CreateDirectory(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                        "Re-Albumizer Backups"));
                    DirectoryInfo currentBackup =
                        backupsDir.CreateSubdirectory($"{SongList[0].Album} - {DateTime.Now:d-M-yy}");
                    new FileInfo(song.fileLoc).CopyTo(Path.Combine(currentBackup.FullName,
                        Path.GetFileName(song.fileLoc)));
                }

            TrySave:
                try
                {
                    song.TaglibFile.Save();
                    processed++;
                }
                catch (IOException IOE)
                {
                    if (MessageBox.Show(IOE.Message, Properties.Resources.WriteFileFailedCaption,
                            MessageBoxButtons.RetryCancel,
                            MessageBoxIcon.Error) == Retry) goto TrySave;
                }

                Thread.Sleep(50);
            }
        }
    }

    private void SaveVal(object? sender, RoutedEventArgs? e)
    {
        ProgressDialog saveDiag = new ProgressDialog();
        saveDiag.WindowTitle = "Saving...";
        saveDiag.ShowCancelButton = false;
        saveDiag.Text = "Applying File Changes";
        saveDiag.DoWork += this.SaveBackground;
        saveDiag.Show();
    }

    private void EditFromListDirect(object sender, RoutedEventArgs e)
    {
        //TODO:Rework For MultiSelect?
        //Maybe, i think it does work??
        //eh?
        MenuItem mi = sender as MenuItem ?? throw new InvalidOperationException();
        ContextMenu cm =
            mi.CommandParameter as ContextMenu ?? throw new InvalidOperationException();
        if (cm.PlacementTarget is StackPanel gridPanel)
        {
            TextBox givenTBox =
                gridPanel.Children.OfType<TextBox>().FirstOrDefault()
                ?? throw new InvalidOperationException();
            TextBlock givenTBlock =
                gridPanel.Children.OfType<TextBlock>().FirstOrDefault()
                ?? throw new InvalidOperationException();
            givenTBlock.Visibility = Visibility.Collapsed;
            givenTBox.Visibility = Visibility.Visible;
        }
        else
        {
            throw new InvalidOperationException("ContextMenu Parent is NOT a StackPanel?? how??");
        }
    }

    private void CheckSubmit(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) this.Submit(sender);
    }

    private void Submit(object sender)
    {
        //i have no clue what to name this text box
        if (sender is TextBox givenTextBox)
        {
            givenTextBox.Visibility = Visibility.Collapsed;
            if (givenTextBox.Parent.GetType() != typeof(StackPanel))
                throw new InvalidOperationException(
                    "Parent of TextBlock Is NOT StackPanel?? This Should not happen"
                );
            StackPanel tbParent = (StackPanel)givenTextBox.Parent;
            // Multiselect Implementation

            foreach (MusicFile song in SongListElement.SelectedItems)
            {
                MusicFile songReal = SongList[SongList.IndexOf(song)];
                switch (tbParent.Name)
                {
                    case "TITLEBOX":
                        songReal.Title = givenTextBox.Text;
                        if ((string)tbParent.Tag == "InPanel") SCtrlTitle.Text = givenTextBox.Text;
                        break;
                    case "ARTISTBOX":
                        songReal.Performers = givenTextBox.Text;
                        if ((string)tbParent.Tag == "InPanel") SCtrlContribArt.Text = givenTextBox.Text;
                        break;
                    case "TRACKBOX":
                        songReal.TrackId = UInt32.Parse(givenTextBox.Text);
                        if ((string)tbParent.Tag == "InPanel") SCtrlTrackNo.Text = givenTextBox.Text;
                        break;
                    case "COMPOSER":
                        songReal.TaglibFile.Tag.Composers = givenTextBox.Text.Split(";");
                        if ((string)tbParent.Tag == "InPanel") SCtrlComposer.Text = givenTextBox.Text;
                        break;
                    case "ALBUMYEAR":
                        try
                        {
                            songReal.TaglibFile.Tag.Year = UInt32.Parse(givenTextBox.Text, NumberStyles.Integer);

                            if ((string)tbParent.Tag == "InPanel") ACtrlYear.Text = givenTextBox.Text;
                        }
                        catch (FormatException)
                        {
                            MessageBox.Show(Properties.Resources.Numerr);
                        }

                        break;
                    case "ALBUMARTIST":
                        songReal.TaglibFile.Tag.AlbumArtists = givenTextBox.Text.Split(";");

                        if ((string)tbParent.Tag == "InPanel") ACtrlMainArtist.Text = givenTextBox.Text;
                        break;
                    case "ALBUMNAME":
                        songReal.TaglibFile.Tag.Album = givenTextBox.Text;

                        if ((string)tbParent.Tag == "InPanel") ACtrlAlbumName.Text = givenTextBox.Text;
                        break;
                    case "ALBUMGENRE":
                        songReal.TaglibFile.Tag.Genres = givenTextBox.Text.Split(";");
                        if ((string)tbParent.Tag == "InPanel") ACtrlGenre.Text = givenTextBox.Text;
                        break;
                    default:
                        MessageBox.Show("something went terribly wrong");
                        return;
                }

                ICollectionView? view = CollectionViewSource.GetDefaultView(SongList);
                view.Refresh();

            #pragma warning disable CS8602 // Dereference of a possibly null reference.
                tbParent.Children
                    .OfType<TextBlock>()
                    .FirstOrDefault()
                #pragma warning restore CS8602
                    .Visibility = Visibility.Visible;
            }
        }
    }

#endregion
}