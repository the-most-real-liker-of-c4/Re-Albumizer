using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using TagLib;
using File = TagLib.File;
using Ookii.Dialogs.Wpf;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using TextBox = System.Windows.Forms.TextBox;
using System.ComponentModel;
using System.Windows.Data;

namespace Re_Albumizer
{
    /// <summary>
    /// A Structure for storing a Song
    /// </summary>
    public class MusicFile
    {
        public uint TrackId { get; set; }
        public string Title { get; set; }
        public string Performers { get; set; }
        public string Album { get; set; }
        public string fileLoc { get; set; }
        public File TaglibFile { get; set; }

        public MusicFile(uint track, string title, string performers, string album, string path, File file)
        {
            TrackId = track;
            Title = title;
            Performers = performers;
            Album = album;
            fileLoc = path;
            TaglibFile = file;
        }

    }
    /// <summary>
    /// Interaction logic for AlbumizerMain.xaml
    /// </summary>
    public partial class AlbumizerMain : Window
    {


        #region Define
        private LinearGradientBrush textBrush = new LinearGradientBrush();
        private SolidColorBrush borderBrush = new SolidColorBrush();
        private string? _path = null;
        public ObservableCollection<MusicFile> SongList = new ObservableCollection<MusicFile>();
        
        #endregion

        #region Album Makers

        public AlbumizerMain()
        {
            
            
            //what???
            InitializeComponent();
            SongListElement.ItemsSource=SongList;
            borderBrush.Color=Color.FromArgb(100,166,214,233);
            textBrush.StartPoint = new Point(0.5, 0);
            textBrush.EndPoint = new Point(0.5, 1);
            textBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 255, 255), 0));
            //textBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 218, 234, 243), 1));
            textBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 198, 227, 239), 1));

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
                if (albumFolderLoader.ShowDialog() == false) return;
                _path = albumFolderLoader.SelectedPath;
                
                LoadedAlbum_Text.Content = "Current Album: " + albumFolderLoader.SelectedPath ?? "None";
                MainAlbumControls.Visibility= Visibility.Visible;
                //Load Files
               

                //Directory.GetFiles(_path, "*.mp3")+(Directory.GetFiles(_path,".MP3"));
                foreach (string file in Directory.GetFiles(_path, "*.mp3"))
                {
                    File currMp3 = TagLib.File.Create(file);
                    //throws null with no tags : Tag currTag = currMp3.GetTag(TagTypes.AllTags,true);
                    //TODO: what if there are no tags??
                    //use addsong below?
                    
                    
                    SongList.Add(new MusicFile(
                        currMp3.Tag.Track,
                        currMp3.Tag.Title,
                        String.Join(",",currMp3.Tag.Performers),
                        currMp3.Tag.Album,
                        file,
                        currMp3));
                    
                }

                foreach (var song in SongList)
                {
                    song.TaglibFile.Tag.TrackCount = (uint)((SongList.Count < 0 ? 0 : SongList.Count) - 1);
                }
               
                //check for no album art before blowing up the program
                //always use the album art of the first song (its easier that way)

                Bitmap? albumArtBitmap = null;
                if (SongList[0].TaglibFile.Tag.Pictures.Length == 0)
                {
                    albumArtBitmap = new Bitmap(Re_Albumizer.Properties.Resources.Nullart);
                }
                else
                {
                    albumArtBitmap = new Bitmap(
                        new MemoryStream(SongList[0].TaglibFile.Tag.Pictures[0].Data.Data)
                    );
                }
                AlbumArt.Source = FromHBitmap(albumArtBitmap);
                //force select to prevent modifying a null song
                SongListElement.SelectedIndex = 0;
                
                
                //you thought we were done here but No!
                ACtrlAlbumName.Text = $"Album: {SongList[0].TaglibFile.Tag.Album}";
                ACtrlGenre.Text = $"Genres: {String.Join(",", SongList[0].TaglibFile.Tag.Genres)}";
                ACtrlMainArtist.Text = $"Album Artists: {String.Join(",",SongList[0].TaglibFile.Tag.AlbumArtists)}";
                ACtrlYear.Text = $"Year: {SongList[0].TaglibFile.Tag.Year.ToString()}";
                AlbumTab.IsEnabled = true;
                SongTab.IsEnabled=true;

            }
            else
            {
                AlbumArt.Source = FromHBitmap(new Bitmap(Re_Albumizer.Properties.Resources.Nullart));
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
            if (selNewFile.ShowDialog() == false) return;
            FileInfo fl = new FileInfo(selNewFile.FileName);
            fl.CopyTo(_path + @"\" + fl.Name);
            File currMp3 = TagLib.File.Create(_path + @"\" + fl.Name);
            if (currMp3.TagTypes == TagTypes.None)
            {
                SongList.Add(new MusicFile(
                    0,
                    Re_Albumizer.Properties.Resources.NoSong,
                    Re_Albumizer.Properties.Resources.NoSong,
                    Re_Albumizer.Properties.Resources.NoSong,
                    _path + @"\" + fl.Name,
                    currMp3));
            }
            else
            {
                SongList.Add(new MusicFile(
                    currMp3.Tag.Track,
                    currMp3.Tag.Title,
                    String.Join(",", currMp3.Tag.Performers),
                    SongList[0].Album,
                    _path + @"\" + fl.Name,
                    currMp3));
            }
            currMp3.Tag.Album = SongList[0].Album;
            currMp3.Tag.Pictures = SongList[0].TaglibFile.Tag.Pictures;
            currMp3.Tag.Genres = SongList[0].TaglibFile.Tag.Genres;
            currMp3.Tag.Year = SongList[0].TaglibFile.Tag.Year;
            currMp3.Tag.AlbumArtists = SongList[0].TaglibFile.Tag.AlbumArtists;
            currMp3.Save();
            fl.Delete();
            foreach (var song in SongList)
            {
                song.TaglibFile.Tag.TrackCount = (uint)((SongList.Count < 0 ? 0 : SongList.Count) - 1);
            }
        }
        #endregion

        #region Album Control Setters
        private void AAlbumNameChange(object sender, RoutedEventArgs e)
        {
            EditObject edobj = new EditObject(EditObject.InputMode.TEXT)
            {
                Title = "Enter New Album Name"
            };
            string res = edobj.ShowForm();
            if (res != "")
            {
                foreach (MusicFile song in SongList)
                {
                    song.TaglibFile.Tag.Album = res;
                    song.Album = res;
                    ACtrlAlbumName.Text = $"Album: {res}";
                    song.TaglibFile.Save();
                }
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
            {
                foreach (MusicFile song in SongList)
                {
                    if (!uint.TryParse(res, NumberStyles.Integer, null, out uint year))
                    {


                        if (MessageBox.Show(Properties.Resources.YearChangeYearMalformatText,
                                Properties.Resources.YearChangeYearMalformatCaption, MessageBoxButtons.OK,
                                MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.OK) return;
                    }

                    song.TaglibFile.Tag.Year = year;
                    ACtrlYear.Text = $"Year: {res}";
                    song.TaglibFile.Save();
                }
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
            {
                foreach (MusicFile song in SongList)
                {
                    song.TaglibFile.Tag.AlbumArtists = res;
                    ACtrlMainArtist.Text = $"Album Artists: {String.Join(",", res)}";
                    song.TaglibFile.Save();
                }
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
            {
                foreach (MusicFile song in SongList)
                {
                    song.TaglibFile.Tag.Genres = res;
                    ACtrlGenre.Text = $"Genres: {String.Join(",", res)}";
                    song.TaglibFile.Save();
                }
            }
        }



        #endregion
        #region Auxillary Functions
        //allows setting ImageSource from Bitmap
        public BitmapSource FromHBitmap(Bitmap bmp)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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

            if (_path != null) Process.Start("explorer.exe", _path);
        }

        private void RemoveItemFromAlbum(object sender, RoutedEventArgs e)
        {
            SongList[SongListElement.SelectedIndex].TaglibFile.RemoveTags(TagTypes.AllTags);
            SongList[SongListElement.SelectedIndex].TaglibFile.Save();
            SongList.RemoveAt(SongListElement.SelectedIndex);
            foreach (var song in SongList)
            {
                song.TaglibFile.Tag.TrackCount = (uint)((SongList.Count < 0 ? 0 : SongList.Count) - 1);
            }

        }

        private void DeleteSongFromDisk(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(string.Format(Properties.Resources.DeleteConfirmText, SongList[SongListElement.SelectedIndex].Title),
                    Properties.Resources.DeleteConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
            {
                new FileInfo(SongList[SongListElement.SelectedIndex].fileLoc).Delete();
                SongList.RemoveAt(SongListElement.SelectedIndex);
                foreach (var song in SongList)
                {
                    song.TaglibFile.Tag.TrackCount = (uint)((SongList.Count < 0 ? 0 : SongList.Count) - 1);
                }
            }
        }

        //Textbox Onhover Helper
        private void CtrlHoverOn(object sender, MouseEventArgs e)
        {
            
            var sendingTextbox = (sender as TextBlock);
            var sendingParent = (sendingTextbox.Parent as Border);
            sendingTextbox.Background = textBrush;
            sendingParent.BorderBrush = borderBrush;
        }
        private void CtrlHoverOff(object sender, MouseEventArgs e)
        {
            var sendingTextbox = (sender as TextBlock);
            var sendingParent = (sendingTextbox.Parent as Border);
            sendingTextbox.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0,0,0,0));
            sendingParent.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0)); ;
        }

        #endregion

        #region Song Control Handlers
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
                    song.TaglibFile.Tag.Title=res;
                    song.Title = res;
                    SCtrlTitle.Text = $"Title: {res}";
                    song.TaglibFile.Save();
                    ICollectionView view = CollectionViewSource.GetDefaultView(SongList);
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
                if (!uint.TryParse(res, NumberStyles.Integer, null, out uint Track))
                {


                    if (MessageBox.Show(Properties.Resources.InvalidTrackNo,
                            Properties.Resources.YearChangeYearMalformatCaption, MessageBoxButtons.OK,
                            MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.OK) return;
                }
                MusicFile song = SongList[SongListElement.SelectedIndex];
                song.TaglibFile.Tag.Track = Track;
                song.TrackId = Track;
                SCtrlTrackNo.Text = $"Track No.: {res}";
                song.TaglibFile.Save();
                ICollectionView view = CollectionViewSource.GetDefaultView(SongList);
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
                    song.TaglibFile.Save();
                    ICollectionView view = CollectionViewSource.GetDefaultView(SongList);
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
                song.TaglibFile.Save();
                ICollectionView view = CollectionViewSource.GetDefaultView(SongList);
                view.Refresh();
            }
        }
        private void OnSongSelectChanged(object sender, SelectionChangedEventArgs e)
        {
            SCtrlTitle.Text = $"Title: {SongList[SongListElement.SelectedIndex].Title}";
            SCtrlTrackNo.Text = $"Track No.: {SongList[SongListElement.SelectedIndex].TrackId}";
            SCtrlContribArt.Text = $"Artists: {String.Join(",", SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Performers)}";
            SCtrlComposer.Text = $"Composers: {String.Join(",", SongList[SongListElement.SelectedIndex].TaglibFile.Tag.Composers)}";
        }
        #endregion
    }
}
