using System.Windows;
using System.Reflection;
using System;

namespace PictureViewer {
	public partial class AboutWindow : Window {
		public AboutWindow() {
			InitializeComponent();

			Assembly app = Assembly.GetExecutingAssembly();
			AssemblyTitleAttribute title = (AssemblyTitleAttribute)app.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0];
			AssemblyDescriptionAttribute desc = (AssemblyDescriptionAttribute)app.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0];

			Version ver = app.GetName().Version;

			txtDesc.Text = title.Title + "\nVersion " + ver.ToString() + "\n\n" + desc.Description;
		}

		private void OK_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}
	}
}
