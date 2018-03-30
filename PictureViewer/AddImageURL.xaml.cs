using System;
using System.Windows;

namespace PictureViewer {
	public partial class AddImageURL : Window {
		public Uri Address {
			get;
			private set;
		}

		public AddImageURL() {
			InitializeComponent();
			Address = null;
			txtURL.Focus();
		}

		private void OK_Clicked(object sender, RoutedEventArgs e) {
			String url = txtURL.Text;
			bool res = Uri.TryCreate(url, UriKind.Absolute, out Uri outUri)
				&& (outUri.Scheme == Uri.UriSchemeHttp
				|| outUri.Scheme == Uri.UriSchemeHttps);

			if (!res) {
				MessageBox.Show("Invalid Address", "Invalid Address");
			} else {
				Address = outUri;
				this.Close();
			}
		}
	}
}
