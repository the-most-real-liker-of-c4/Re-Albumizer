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

namespace Re_Albumizer
{
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

        

        
        private string? _path = null;
        public ObservableCollection<dynamic> SongList = new ObservableCollection<dynamic>();
        
        #endregion

        #region Album Modifiers

        public AlbumizerMain()
        {
            
            //what???
            InitializeComponent();
            SongListElement.ItemsSource=SongList;
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

                SongList[0].TaglibFile.Tag.TrackCount = (uint)((SongList.Count < 0?0:SongList.Count) - 1);
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
                //force select to prevent playing a null song
                SongListElement.SelectedIndex = 0;
                
                
                //you thought we were done here but No!
                ACtrlAlbumName.Text = SongList[0].TaglibFile.Tag.Album;
                ACtrlGenre.Text = String.Join(",",SongList[0].TaglibFile.Tag.Genres);
                ACtrlMainArtist.Text = String.Join(",",SongList[0].TaglibFile.Tag.AlbumArtists);
                ACtrlYear.Text = SongList[0].TaglibFile.Tag.Year.ToString();


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
            File currMp3 = TagLib.File.Create(selNewFile.FileName);
            if (currMp3.TagTypes != TagTypes.AllTags)
            {
                SongList.Add(new MusicFile(
                    0,
                    Re_Albumizer.Properties.Resources.NoSong,
                    Re_Albumizer.Properties.Resources.NoSong,
                    Re_Albumizer.Properties.Resources.NoSong,
                    selNewFile.FileName,
                    currMp3));
            }
            else
            {
                SongList.Add(new MusicFile(
                    currMp3.Tag.Track,
                    currMp3.Tag.Title,
                    String.Join(",", currMp3.Tag.Performers),
                    currMp3.Tag.Album,
                    selNewFile.FileName,
                    currMp3));
            }
            SongList[0].TaglibFile.Tag.TrackCount = (SongList.Count < 0 ? 0 : SongList.Count) - 1;
        }

        private void OpenAlbumFolder(object sender, RoutedEventArgs e)
        {
            if (_path != null) Process.Start("explorer.exe", _path);
        }

        private void RemoveitemfromAlbum(object sender, RoutedEventArgs e)
        {
            SongList[SongListElement.SelectedIndex].SongFile.RemoveTags(TagTypes.AllTags);
            SongList.RemoveAt(SongListElement.SelectedIndex);
            SongList[0].TaglibFile.Tag.TrackCount = (SongList.Count < 0 ? 0 : SongList.Count) - 1;

        }

        private void DeleteSongFromDisk(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(string.Format(Properties.Resources.DeleteConfirmText, SongList[SongListElement.SelectedIndex].Name),
                    Properties.Resources.DeleteConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
            {
                new FileInfo(SongList[SongListElement.SelectedIndex].Path).Delete();
                SongList.RemoveAt(SongListElement.SelectedIndex);
                SongList[0].TaglibFile.Tag.TrackCount = (SongList.Count < 0 ? 0 : SongList.Count) - 1;
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
        private void AAlbumNameChange(object sender, RoutedEventArgs e)
        {
            EditObject edobj = new EditObject(EditObject.InputMode.TEXT);
            edobj.Title = "Enter New Album Name";
            string res = edobj.ShowForm();
            foreach (MusicFile song in SongList)
            {
                song.TaglibFile.Tag.Album = res;
                song.Album = res;
                ACtrlAlbumName.Text = res;
                song.TaglibFile.Save();
            }
        }

        private void AYearChange(object sender, RoutedEventArgs e)
        {
            EditObject edobj = new EditObject(EditObject.InputMode.TEXT);
            edobj.Title = "Enter Album Year";
            string res = edobj.ShowForm();
            MessageBox.Show(res);
            foreach (MusicFile song in SongList)
            {
                try
                {
                    song.TaglibFile.Tag.Year = uint.Parse(res);
                    ACtrlYear.Text = res;
                    song.TaglibFile.Save();
                }
                catch (FormatException ex)
                {
                    if (MessageBox.Show(Properties.Resources.YearChangeYearMalformatText,
                            Properties.Resources.YearChangeYearMalformatCaption, MessageBoxButtons.RetryCancel,
                            MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Cancel) return;
                    AYearChange(sender,e);
                }

                
            }
        }

        private void AArtistChange(object sender, RoutedEventArgs e)
        {
            EditObject edobj = new EditObject(EditObject.InputMode.ARRAYTEXT);
            edobj.Title = "Enter Artists Name";
            string[] res = edobj.ShowForm();
            foreach (MusicFile song in SongList)
            {
                song.TaglibFile.Tag.AlbumArtists=res;
                ACtrlMainArtist.Text = String.Join(",", res);
                song.TaglibFile.Save();
            }
        }

        private void AGenreChange(object sender, RoutedEventArgs e)
        {
            EditObject edobj = new EditObject(EditObject.InputMode.ARRAYTEXT);
            edobj.Title = "Enter Genre";
            string[] res = edobj.ShowForm();
            foreach (MusicFile song in SongList)
            {
                song.TaglibFile.Tag.Genres = res;
                ACtrlGenre.Text = String.Join(",", res);
                song.TaglibFile.Save();
            }
        }
        #endregion


    }
}
