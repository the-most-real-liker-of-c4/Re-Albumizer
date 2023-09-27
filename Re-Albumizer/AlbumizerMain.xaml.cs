using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ookii.Dialogs.Wpf;
using TagLib;
using Color = System.Windows.Media.Color;
using File = TagLib.File;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
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
    private void OnChangeAlbumArt(object sender, RoutedEventArgs e)
    {
        var selNewFile = new VistaOpenFileDialog
        {
            Multiselect = false,
            Title = Properties.Resources.SelNewAlbumArt,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        if (selNewFile.ShowDialog() == false)
            return;
        foreach (var song in SongList)
        {
            AlbumArt.Source = FromHBitmap(new Bitmap(selNewFile.FileName));
            song.TaglibFile.Tag.Pictures = new IPicture[] { new Picture(selNewFile.FileName) };
            song.TaglibFile.Save();
        }
    }

    private void SaveVal(object sender, RoutedEventArgs e)
    {
        foreach (var song in SongList)
        {
            song.TaglibFile.Tag.Performers = new[] { song.Performers };
            song.TaglibFile.Tag.Title = song.Title;
            song.TaglibFile.Tag.Album = song.Album;
            song.TaglibFile.Tag.Track = song.TrackId;
            song.TaglibFile.Save();
        }
    }

    private void EditFromListDirect(object sender, RoutedEventArgs e)
    {
        //TODO:Rework For MultiSelect?
        //Maybe, i think it does work??
        //eh?
        var mi = sender as MenuItem ?? throw new InvalidOperationException();
        var cm =
            mi.CommandParameter as ContextMenu ?? throw new InvalidOperationException();
        if (cm.PlacementTarget is StackPanel gridPanel)
        {
            var givenTBox =
                gridPanel.Children.OfType<TextBox>().FirstOrDefault()
                ?? throw new InvalidOperationException();
            var givenTBlock =
                gridPanel.Children.OfType<TextBlock>().FirstOrDefault()
                ?? throw new InvalidOperationException();
            givenTBlock.Visibility = Visibility.Collapsed;
            givenTBox.Visibility = Visibility.Visible;
        }
        else
        {
            throw new InvalidOperationException("ContextMenu Parent is NOT a StackPanel");
        }
    }

    private void CheckSubmit(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) Submit(sender);
    }

    private void Submit(object sender)
    {
        if (sender is TextBox givenTextBox)
        {
            givenTextBox.Visibility = Visibility.Collapsed;
            if (givenTextBox.Parent.GetType() != typeof(StackPanel))
                throw new InvalidOperationException(
                    "Parent of TextBlock Is NOT StackPanel?? This Should not happen"
                );
            var tbParent = (StackPanel)givenTextBox.Parent;
            //are we under multiselect?
            if (SongListElement.SelectedItems.Count > 1)
                foreach (MusicFile song in SongListElement.SelectedItems)
                {
                    var songReal = SongList[SongList.IndexOf(song)];
                    switch (tbParent.Name)
                    {
                        case "TITLEBOX":

                            songReal.Title = givenTextBox.Text;
                            break;
                        case "ARTISTBOX":
                            songReal.Performers = givenTextBox.Text;
                            break;
                        case "TRACKBOX":
                            songReal.TrackId = uint.Parse(givenTextBox.Text);
                            break;
                        case "ALBUMBOX":
                            songReal.Album = givenTextBox.Text;
                            break;
                        default:
                            MessageBox.Show("something went terribly wrong");
                            return;
                            break;
                    }

                    var view = CollectionViewSource.GetDefaultView(SongList);
                    view.Refresh();
                }


            tbParent.Children
                .OfType<TextBlock>()
                .FirstOrDefault()
                .Visibility = Visibility.Visible;
        }
    }

    //add Unsaved Change Code

    private void CheckFocusSubmit(object sender, RoutedEventArgs e)
    {
        Submit(sender);
    }

    #region Define

    private readonly LinearGradientBrush textBrush = new();
    private readonly SolidColorBrush borderBrush = new();
    private string? _path;
    public ObservableCollection<MusicFile> SongList = new();

    #endregion Define

    #region Album Makers

    public AlbumizerMain()
    {
        //what???
        InitializeComponent();
        SongListElement.ItemsSource = SongList;
        borderBrush.Color = Color.FromArgb(100, 166, 214, 233);
        textBrush.StartPoint = new Point(0.5, 0);
        textBrush.EndPoint = new Point(0.5, 1);
        textBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 255, 255), 0));
        //textBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 218, 234, 243), 1));
        textBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 198, 227, 239), 1));
    }

    private void NewAlbumFolder(object sender, RoutedEventArgs e)
    {
        AlbumArt.Source = FromHBitmap(new Bitmap(Properties.Resources.Nullart));
        //SongList.Clear();
        var edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Album Folder Name"
        };
        string res = edobj.ShowForm();
        if (res != "")
        {
            var albumFolderLoader = new VistaFolderBrowserDialog
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
            var selNewFile = new VistaOpenFileDialog
            {
                Multiselect = false,
                Title = Properties.Resources.selectnewsongPrompt,
                Filter = "*.mp3|*MP3",
                InitialDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile
                )
            };
            if (selNewFile.ShowDialog() == false)
                return;
            var fl = new FileInfo(selNewFile.FileName);
            fl.CopyTo(_path + @"\" + fl.Name);
            var currMp3 = File.Create(_path + @"\" + fl.Name);
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
                        string.Join(",", currMp3.Tag.Performers),
                        currMp3.Tag.Album,
                        _path + @"\" + fl.Name,
                        currMp3
                    )
                );

            fl.Delete();
            foreach (var song in SongList) song.TaglibFile.Tag.TrackCount = (uint)SongList.Count;
        }
    }

    private void LoadNewAlbum(object sender, RoutedEventArgs e)
    {
        EditMenu.IsEnabled = true;
        if (SongListElement.Items.Count == 0)
        {
            //TODO: Load files in correct order using song index
            //Load Folder
            var albumFolderLoader = new VistaFolderBrowserDialog
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
            foreach (var file in Directory.GetFiles(_path, "*.mp3"))
            {
                var currMp3 = File.Create(file);

                SongList.Add(
                    new MusicFile(
                        currMp3.Tag.Track,
                        currMp3.Tag.Title,
                        string.Join(",", currMp3.Tag.Performers),
                        currMp3.Tag.Album,
                        file,
                        currMp3
                    )
                );
            }

            foreach (var song in SongList) song.TaglibFile.Tag.TrackCount = (uint)SongList.Count;

            //check for no album art before blowing up the program
            //always use the album art of the first song (its easier that way)

            Bitmap? albumArtBitmap = null;
            if (SongList[0].TaglibFile.Tag.Pictures.Length == 0)
                albumArtBitmap = new Bitmap(Properties.Resources.Nullart);
            else
                albumArtBitmap = new Bitmap(
                    new MemoryStream(SongList[0].TaglibFile.Tag.Pictures[0].Data.Data)
                );

            AlbumArt.Source = FromHBitmap(albumArtBitmap);
            //force select to prevent modifying a null song
            SongListElement.SelectedIndex = 0;

            //you thought we were done here but No!
            ACtrlAlbumName.Text = $"Album: {SongList[0].TaglibFile.Tag.Album}";
            ACtrlGenre.Text = $"Genres: {string.Join(",", SongList[0].TaglibFile.Tag.Genres)}";
            ACtrlMainArtist.Text =
                $"Album Artists: {string.Join(",", SongList[0].TaglibFile.Tag.AlbumArtists)}";
            ACtrlYear.Text = $"Year: {SongList[0].TaglibFile.Tag.Year.ToString()}";
            AlbumTab.IsEnabled = true;
            SongTab.IsEnabled = true;
        }
        else
        {
            AlbumArt.Source = FromHBitmap(
                new Bitmap(Properties.Resources.Nullart)
            );
            SongList.Clear();
            //possible LOOP!!! (shouldn't occur)
            LoadNewAlbum(sender, e);
        }
    }

    private void AddSongToAlbum(object sender, RoutedEventArgs e)
    {
        var selNewFile = new VistaOpenFileDialog
        {
            Multiselect = false,
            Title = Properties.Resources.selectnewsongPrompt,
            Filter = "*.mp3|*MP3",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        if (selNewFile.ShowDialog() == false)
            return;
        var fl = new FileInfo(selNewFile.FileName);
        fl.CopyTo(_path + @"\" + fl.Name);
        var currMp3 = File.Create(_path + @"\" + fl.Name);
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
                    string.Join(",", currMp3.Tag.Performers),
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
        currMp3.Save();
        fl.Delete();
        foreach (var song in SongList) song.TaglibFile.Tag.TrackCount = (uint)SongList.Count;
    }

    #endregion Album Makers

    #region Album Control Setters

    private void AAlbumNameChange(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Album Name"
        };
        string res = edobj.ShowForm();
        if (res != "")
            foreach (var song in SongList)
            {
                song.TaglibFile.Tag.Album = res;
                song.Album = res;
                ACtrlAlbumName.Text = $"Album: {res}";
                song.TaglibFile.Save();
            }
    }

    private void AYearChange(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter Album Year"
        };
        string res = edobj.ShowForm();
        //MessageBox.Show(int.Parse(res).ToString());
        if (res != "")
            foreach (var song in SongList)
            {
                if (!uint.TryParse(res, NumberStyles.Integer, null, out var year))
                    if (
                        MessageBox.Show(
                            Properties.Resources.YearChangeYearMalformatText,
                            Properties.Resources.YearChangeYearMalformatCaption,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        ) == System.Windows.Forms.DialogResult.OK
                    )
                        return;

                song.TaglibFile.Tag.Year = year;
                ACtrlYear.Text = $"Year: {res}";
                song.TaglibFile.Save();
            }
    }

    private void AArtistChange(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Artists Name"
        };
        string[] res = edobj.ShowForm();
        //MessageBox.Show(res);
        if (res.Length != 0)
            foreach (var song in SongList)
            {
                song.TaglibFile.Tag.AlbumArtists = res;
                ACtrlMainArtist.Text = $"Album Artists: {string.Join(",", res)}";
                song.TaglibFile.Save();
            }
    }

    private void AGenreChange(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Genre"
        };
        string[] res = edobj.ShowForm();
        if (res.Length != 0)
            foreach (var song in SongList)
            {
                song.TaglibFile.Tag.Genres = res;
                ACtrlGenre.Text = $"Genres: {string.Join(",", res)}";
                song.TaglibFile.Save();
            }
    }

    #endregion Album Control Setters

    #region Auxillary Functions

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

    public void ExitApp(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private void OpenAboutPage(object sender, RoutedEventArgs e)
    {
        //show about page
        new EditObject(IntPtr.Zero).ShowDialog();
    }

    private void OpenAlbumFolder(object sender, RoutedEventArgs e)
    {
        if (_path != null)
            Process.Start("explorer.exe", _path);
    }

    private void RemoveItemFromAlbum(object sender, RoutedEventArgs e)
    {
        SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Album = "";
        SongList[SongListElement.SelectedIndex].TaglibFile.Save();
        SongList.RemoveAt(SongListElement.SelectedIndex);

        foreach (var song in SongList) song.TaglibFile.Tag.TrackCount = (uint)SongList.Count;
    }

    private void DeleteSongFromDisk(object sender, RoutedEventArgs e)
    {
        if (
            MessageBox.Show(
                string.Format(
                    Properties.Resources.DeleteConfirmText,
                    SongList[SongListElement.SelectedIndex].Title
                ),
                Properties.Resources.DeleteConfirmCaption,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button2
            ) == System.Windows.Forms.DialogResult.Yes
        )
        {
            new FileInfo(SongList[SongListElement.SelectedIndex].fileLoc).Delete();
            SongList.RemoveAt(SongListElement.SelectedIndex);
            foreach (var song in SongList) song.TaglibFile.Tag.TrackCount = (uint)SongList.Count;
        }
    }

    //Textbox Onhover Helper
    private void CtrlHoverOn(object sender, MouseEventArgs e)
    {
        var sendingTextbox = sender as TextBlock;
        var sendingParent = sendingTextbox.Parent as Border;
        sendingTextbox.Background = textBrush;
        sendingParent.BorderBrush = borderBrush;
    }

    private void CtrlHoverOff(object sender, MouseEventArgs e)
    {
        var sendingTextbox = sender as TextBlock;
        var sendingParent = sendingTextbox.Parent as Border;
        sendingTextbox.Background = new SolidColorBrush(
            Color.FromArgb(0, 0, 0, 0)
        );
        sendingParent.BorderBrush = new SolidColorBrush(
            Color.FromArgb(0, 0, 0, 0)
        );
        ;
    }

    #endregion Auxillary Functions

    #region Song Control Handlers

    //TODO:TODO-ING:change to Native WMP style edit
    private void OnSCtrlTitle(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Song Title"
        };
        string res = edobj.ShowForm();
        if (res != "")
        {
            var song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Title = res;
            song.Title = res;
            SCtrlTitle.Text = $"Title: {res}";
            song.TaglibFile.Save();
            //Refresh ListView
            var view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }

    private void OnSCtrlTrackNo(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.TEXT)
        {
            Title = "Enter New Track Number"
        };
        string res = edobj.ShowForm();
        if (res != "")
        {
            if (!uint.TryParse(res, NumberStyles.Integer, null, out var Track))
                if (
                    MessageBox.Show(
                        Properties.Resources.InvalidTrackNo,
                        Properties.Resources.YearChangeYearMalformatCaption,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    ) == System.Windows.Forms.DialogResult.OK
                )
                    return;

            var song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Track = Track;
            song.TrackId = Track;
            SCtrlTrackNo.Text = $"Track No.: {res}";
            song.TaglibFile.Save();
            var view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }

    private void OnSCtrlContribArt(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Artists Name"
        };
        string[] res = edobj.ShowForm();
        //MessageBox.Show(res);
        if (res.Length != 0)
        {
            var song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Performers = res;
            song.Performers = string.Join(",", res);
            SCtrlContribArt.Text = $"Artists: {string.Join(",", res)}";
            song.TaglibFile.Save();
            var view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }

    private void OnSCtrlComposer(object sender, RoutedEventArgs e)
    {
        var edobj = new EditObject(EditObject.InputMode.ARRAYTEXT)
        {
            Title = "Enter Composer Name"
        };
        string[] res = edobj.ShowForm();
        //MessageBox.Show(res);
        if (res.Length != 0)
        {
            var song = SongList[SongListElement.SelectedIndex];
            song.TaglibFile.Tag.Composers = res;
            SCtrlComposer.Text = $"Composers: {string.Join(",", res)}";
            song.TaglibFile.Save();
            var view = CollectionViewSource.GetDefaultView(SongList);
            view.Refresh();
        }
    }

    private void OnSongSelectChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (SongList.Count != 0)
            {
                SCtrlTitle.Text = $"Title: {SongList[SongListElement.SelectedIndex].Title}";
                SCtrlTrackNo.Text =
                    $"Track No.: {SongList[SongListElement.SelectedIndex].TrackId}";
                SCtrlContribArt.Text =
                    $"Artists: {string.Join(",", SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Performers)}";
                SCtrlComposer.Text =
                    $"Composers: {string.Join(",", SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Composers)}";
            }
        }
        catch (Exception)
        {
            // just ignore it and redirect to zero
            SongListElement.SelectedIndex = 0;
        }
    }

    #endregion Song Control Handlers
}