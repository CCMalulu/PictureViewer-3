/**
 * Picture Viewer
 * Neil Brommer
 * 
 * Note: the window will not resize itself to the full size of large images.
 * This is due to Windows preventing the window from growing too large.
 */

using Microsoft.Win32;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PictureViewer {
	public partial class MainWindow : Window {
		private List<String> queue;
		private int curImage;
		private double delayTime;
		private String tempDir;
		private DispatcherTimer timer;
		private GridLength sidebarWidth;
		private int nextImage;
		private TimeSpan animationDuration;

		public MainWindow() {
			InitializeComponent();
			queue = new List<String>();
			curImage = -1;
			delayTime = 5.0;
			tempDir = Environment.GetEnvironmentVariable("TEMP");
			timer = new DispatcherTimer {
				Interval = new TimeSpan(0, 0, 1)
			};
			timer.Tick += Timer_Tick;
			this.sidebarWidth = this.colQueue.Width; // initialize it just in case
			this.nextImage = -1;
			animationDuration = TimeSpan.FromSeconds(0.25);
		}

		private void ImgMain_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

				AddFiles(files);
			} else if (e.Data.GetDataPresent(DataFormats.Text)) {
				string url = (String)e.Data.GetData(DataFormats.Text);
				AddURL(url);
			}
		}

		private void OpenFiles_Clicked(object sender, RoutedEventArgs e) {
			OpenFileDialog dlg = new OpenFileDialog {
				Title = "Open Images",
				Multiselect = true,
				DefaultExt = ".png",
				Filter = "Image Files|*.jpeg;*.jpg;*.png;*.gif;*.bmp;*.ico|All files (*.*)|*.*"
			};
			bool? res = dlg.ShowDialog();

			if (res != null && res == true) {
				AddFiles(dlg.FileNames);
			}
		}

		private void LoadURL_Clicked(object sender, RoutedEventArgs e) {
			AddImageURL dlg = new AddImageURL();
			dlg.ShowDialog();
			Uri address = dlg.Address;

			if (address != null)
				AddURL(address.AbsoluteUri);
		}

		private void Start_Clicked(object sender, RoutedEventArgs e) {
			if (queue.Count > 0 && curImage < queue.Count - 1) {
				timer.Start();
				btnStart.IsEnabled = false;
				btnPause.IsEnabled = true;
			}  else
				MessageBox.Show("Finished Queue", "Finished");
		}

		private void Pause_Clicked(object sender, RoutedEventArgs e) {
			timer.Stop();
			btnPause.IsEnabled = false;
			btnStart.IsEnabled = true;
		}

		private void Timer_Tick(object sender, EventArgs e) {
			progressBar.Value = progressBar.Value + (1.0 / delayTime * 1000);
			if (progressBar.Value == progressBar.Maximum) {
				Next_Clicked(null, null);
				progressBar.Value = 0;

				if (curImage == queue.Count - 2)
					Pause_Clicked(null, null);
			}
		}

		private void ClearQueue_Clicked(object sender, RoutedEventArgs e) {
			imageQueue.Items.Clear();
			queue.Clear();
			EnableUI(false);

			this.SizeToContent = SizeToContent.Manual;
			if (this.chkResizeWindow.IsChecked == true) {
				this.Width = 500;
				this.Height = 350;
			}

			imgMain.Source = new BitmapImage(new Uri("pack://application:,,,/PictureViewer;component/Resources/image.ico", UriKind.Absolute));
			imgMain.Stretch = Stretch.None;
			curImage = -1;

			if (timer.IsEnabled) {
				timer.Stop();
				progressBar.Value = 0;
			}

			SetStatus("Ready");
		}

		private void Previous_Clicked(object sender, RoutedEventArgs e) {
			if (curImage > 0 && curImage < queue.Count) {
				SetImage(curImage - 1);
			}
		}

		private void Next_Clicked(object sender, RoutedEventArgs e) {
			if (curImage < queue.Count - 1 && curImage >= 0) {
				SetImage(curImage + 1);
			}
		}

		private void ImageQueue_Clicked(object sender, RoutedEventArgs e) {
			int index = imageQueue.Items.IndexOf((UIElement)((Image)sender).Parent);
			SetImage(index);
		}

		private void SetImage(int index) {
			if (index >= imageQueue.Items.Count || index < 0)
				throw new IndexOutOfRangeException("index given to SetImage is out of range");

			this.nextImage = index;

			// fade the current image out
			DoubleAnimation animation = new DoubleAnimation(1, 0, this.animationDuration);
			animation.Completed += SetNewImage;
			this.canvas.BeginAnimation(Image.OpacityProperty, animation);
		}

		private void SetNewImage(object sender, EventArgs e) {
			// set the new image
			BitmapImage newMain = null;
			try {
				newMain = new BitmapImage(new Uri(queue[this.nextImage]));
			} catch (FileNotFoundException ex) {
				MessageBox.Show("File bot found: " + ex.FileName, "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			if (newMain == null) {
				RemoveImage(this.nextImage);
				return;
			}

			imgMain.Source = newMain;

			// deselect the old thumbnail in the queue
			StackPanel thumb;
			if (curImage != -1) {
				thumb = (StackPanel)imageQueue.Items[curImage];
				thumb.Background = Brushes.Transparent;
			}

			this.curImage = this.nextImage;

			// select the new thumbnail in the queue
			thumb = (StackPanel)this.imageQueue.Items[this.curImage];
			thumb.Background = SystemColors.HighlightBrush;

			SetStatus(queue[this.curImage]);

			// size window or image to show the full image
			if (this.chkResizeWindow.IsChecked == true) {
				double windowExtrasX = this.ActualWidth - this.canvas.ActualWidth;
				double windowExtrasY = this.ActualHeight - this.canvas.ActualHeight;

				this.Width = windowExtrasX + newMain.Width;
				this.Height = windowExtrasY + newMain.Height;

				Console.WriteLine("WindowX: " + this.ActualWidth + ", WindowY: " + this.ActualHeight);
				Console.WriteLine("ImageX: " + newMain.Width + ", ImageY: " + newMain.Height);
				Console.WriteLine();
			} else {
				if (this.chkFitToWindow.IsChecked == true)
					this.OnCanvasResize(null, null); // fit the image to the window
			}

			// scroll the current thumbnail into view
			if (imageQueue.ItemContainerGenerator.ContainerFromIndex(curImage) is FrameworkElement container) {
				container.BringIntoView();
			}

			// fade the new image in
			DoubleAnimation animation = new DoubleAnimation(0, 1, this.animationDuration);
			this.canvas.BeginAnimation(Image.OpacityProperty, animation);
		}

		private void RemoveImage_Clicked(object sender, RoutedEventArgs e) {
			if (sender is MenuItem mnu) {
				Image img = ((ContextMenu)mnu.Parent).PlacementTarget as Image;
				int index = imageQueue.Items.IndexOf(img);
				this.RemoveImage(index);
			}
		}

		private void RemoveImage(int index) {
			if (index == curImage || curImage > index)
				curImage--;

			imageQueue.Items.RemoveAt(index);
			queue.RemoveAt(index);

			if (queue.Count == 0)
				ClearQueue_Clicked(null, null);
			else
				SetImage(curImage);
		}

		private void AddFiles(string[] files) {
			foreach (String filename in files) {
				try {
					StackPanel panel = new StackPanel();
					Image img = new Image();
					BitmapImage bi = new BitmapImage();
					bi.BeginInit();
					bi.CacheOption = BitmapCacheOption.OnLoad;
					bi.DecodePixelWidth = 300;
					bi.UriSource = new Uri(filename);
					bi.EndInit();
					img.Source = bi;
					img.Stretch = Stretch.Uniform;
					if (queue.Count == 0)
						img.Margin = new Thickness(5, 5, 5, 5);
					else
						img.Margin = new Thickness(5, 5, 5, 5);
					img.MouseLeftButtonUp += ImageQueue_Clicked;
					img.ContextMenu = this.Resources["imageMenu"] as ContextMenu;
					panel.Children.Add(img);
					imageQueue.Items.Add(panel);
					queue.Add(filename);
				} catch (NotSupportedException) {
					MessageBox.Show(filename + " is not in a supported format", "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Error);
				} catch (FileNotFoundException) {
					MessageBox.Show(filename + " was not found", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			if (imageQueue.Items.Count > 0 && curImage == -1) {
				SetImage(0);
				imgMain.Stretch = Stretch.Uniform;
				EnableUI(true);
			}
		}

		private void AddURL(string url) {
			bool res = Uri.TryCreate(url, UriKind.Absolute, out Uri address)
					&& (address.Scheme == Uri.UriSchemeHttp
					|| address.Scheme == Uri.UriSchemeHttps);
			
			if (res) {
				String localPath = Path.Combine(tempDir, Path.GetFileName(address.LocalPath));

				Console.WriteLine("Got this far");

				WebClient client = new WebClient();
				try {
					client.DownloadFile(address, localPath);
					AddFiles(new string[] { localPath });
				} catch (WebException ex) {
					MessageBox.Show("Error loading image:\n" + ex.Message, "Error");
				}
			}
		}

		private void EnableUI(bool en) {
			btnClear.IsEnabled = en;
			btnStart.IsEnabled = en;
			btnPrev.IsEnabled = en;
			btnNext.IsEnabled = en;

			mnuClear.IsEnabled = en;
			mnuStart.IsEnabled = en;
			mnuPrev.IsEnabled = en;
			mnuNext.IsEnabled = en;

			if (!en) { // don't automatically enable these
				btnPause.IsEnabled = false;
				mnuPause.IsEnabled = false;
			}
		}

		private void SetImageToActualSize() {
			this.imgMain.Width = Double.NaN; // set to auto
			this.imgMain.Height = Double.NaN;

			this.canvas.Width = Double.NaN;
			this.canvas.Height = Double.NaN;
		}

		private void SetStatus(string status) {
			statusText.Text = status;
		}

		private void About_Clicked(object sender, RoutedEventArgs e) {
			AboutWindow abt = new AboutWindow();
			abt.Show();
		}

		private void Exit_Clicked(object sender, RoutedEventArgs e) {
			this.Close();
		}

		private void OnShowQueueChecked(object sender, RoutedEventArgs e) {
			if (this.IsLoaded) {
				if (this.chkShowQueue.IsChecked == true) {
					this.mnuShowQueue.IsChecked = true;
					this.queueSidebar.Visibility = Visibility.Visible;
					this.colQueue.Width = sidebarWidth;
					this.colQueue.MinWidth = 50;
					this.colSplitter.IsEnabled = true;
				} else {
					this.mnuShowQueue.IsChecked = false;
					this.sidebarWidth = this.colQueue.Width;
					this.colQueue.MinWidth = 0;
					this.colSplitter.IsEnabled = false;
					this.queueSidebar.Visibility = Visibility.Collapsed;
					this.colQueue.Width = new GridLength(0);
				}
			}
		}

		private void OnCanvasResize(object sender, SizeChangedEventArgs e) {
			// TODO scale the canvas/image to fit the window
			this.imgMain.Width = this.canvas.ActualWidth;
			this.imgMain.Height = this.canvas.ActualHeight;
		}

		private void OnChkFitToWindowChecked(object sender, RoutedEventArgs e) {
			if (this.IsLoaded) {
				if (this.chkFitToWindow.IsChecked == true) {
					this.mnuFitImage.IsChecked = true;
					this.canvas.SizeChanged += OnCanvasResize;
					this.OnCanvasResize(null, null);
				} else {
					this.mnuFitImage.IsChecked = false;
					this.canvas.SizeChanged -= OnCanvasResize;
					this.SetImageToActualSize();
				}
			}
		}

		private void OnChkResizeWindowChecked(object sender, RoutedEventArgs e) {
			if (this.IsLoaded) {
				if (this.chkResizeWindow.IsChecked == true)
					this.mnuResizeWindow.IsChecked = true;
				else
					this.mnuResizeWindow.IsChecked = false;
			}
		}

		private void OnMnuResizeWindowChecked(object sender, RoutedEventArgs e) {
			if (this.IsLoaded)
				this.chkResizeWindow.IsChecked = this.mnuResizeWindow.IsChecked;
		}

		private void OnMnuFitImageChecked(object sender, RoutedEventArgs e) {
			if (this.IsLoaded)
				this.chkFitToWindow.IsChecked = this.mnuFitImage.IsChecked;
		}

		private void OnMnuShowQueueChecked(object sender, RoutedEventArgs e) {
			if (this.IsLoaded)
				this.chkShowQueue.IsChecked = this.mnuShowQueue.IsChecked;
		}

		private void OnChangeAnimLength_Clicked(object sender, RoutedEventArgs e) {
			AskForTime ask = new AskForTime(this.animationDuration.TotalSeconds, "Animation", "double");
			ask.ShowDialog();
			if (ask.DialogResult == true) {
				this.animationDuration = TimeSpan.FromSeconds(ask.time);
			}
		}

		private void OnChangeSlideDelay_Clicked(object sender, RoutedEventArgs e) {
			AskForTime ask = new AskForTime(this.delayTime, "Slide Delay", "int");
			ask.ShowDialog();
			if (ask.DialogResult == true) {
				this.delayTime = ask.time;
			}
		}
	}
}
