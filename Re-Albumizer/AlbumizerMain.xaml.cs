using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TagLib;
using File = TagLib.File;
using Ookii.Dialogs.Wpf;
using System.Windows.Forms;
 
namespace Re_Albumizer
{
    public struct MusicFile 
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public TimeSpan Runningtime { get; set; }
        public File SongFile { get; set; }

        public MusicFile(string path, string name, TimeSpan time, TagLib.File mp3Object)
        {
            Path = path;
            Name = name;
            Runningtime = time;

            SongFile = mp3Object;
        }

    }
    /// <summary>
    /// Interaction logic for AlbumizerMain.xaml
    /// </summary>
    public partial class AlbumizerMain : Window
    {
        #region Define

        

        
        private string? _path = null;
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")] 
        public List<dynamic> _songList = new List<dynamic>();
        
        #endregion

        #region Album Loaders

        public AlbumizerMain()
        {
            //what???
            this.DataContext = this;
            InitializeComponent();
            SongList.ItemsSource = _songList;
        }

        private void LoadNewAlbum(object sender, RoutedEventArgs e)
        {
            EditMenu.IsEnabled = true;
            if (SongList.Items.Count == 0)
            {
                //TODO: Load files in correct order using song index
                //Load Folder
                var albumFolderLoader = new VistaFolderBrowserDialog
                {
                    UseDescriptionForTitle = true,
                    Description = "Select Album Root Folder"
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
                    //throws null with no tags Tag currTag = currMp3.GetTag(TagTypes.AllTags,true);
                    //TODO: what if there are no tags??

                    // the two arrays should have the same index for a given item (eg. Splattack! - Squid Squad at index zero on both) this is a dirty trick that will help us later.
                    _songList.Add(new
                    {
                        TrackId=currMp3.Tag.Track,
                        Title= currMp3.Tag.Title,
                        Performers= String.Join(",", currMp3.Tag.Performers),
                        Album=currMp3.Tag.Album,
                        fileLoc=file,
                        TaglibFile=currMp3

                    });
                    



                }

                //check for no album art before blowing up the program
                //always use the first album art of the first song (its easier that way)

                Bitmap? albumArtBitmap = null;
                if (_songList[0].TaglibFile.Tag.Pictures.Length == 0)
                {
                    albumArtBitmap = new Bitmap(Re_Albumizer.Properties.Resources.Nullart);
                }
                else
                {
                    albumArtBitmap = new Bitmap(
                        new MemoryStream(_songList[0].TaglibFile.Tag.Pictures[0].Data.Data)
                    );
                }
                
                
                AlbumArt.Source = FromHBitmap(albumArtBitmap);

                //this code is meant for calculating the horizontalextent property and is now kept for historical reasons
                /*
                string[] lengthList = new string[SongList.Items.Count];
                SongList.Items.CopyTo(lengthList, 0);
                Array.Sort(lengthList, (x, y) => x.Length.CompareTo(y.Length));
                LengthFinder.Content = lengthList[^1];
                SongList.HorizontalExtent = LengthFinder.Width;
                */
                //force select to prevent playing a null song
                SongList.SelectedIndex = 0;
                //PlaySong.Visible = true;
                //_UpdateControls();
                
            }
            else
            {
                AlbumArt.Source = FromHBitmap(new Bitmap(Re_Albumizer.Properties.Resources.Nullart));
                _songList.Clear();
                SongList.Items.Clear();
                //possible LOOP!!! (shouldn't occur)
                LoadNewAlbum(sender, e);
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
        }
        #endregion

        private void SaveAlbumFile(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        
    }
}
