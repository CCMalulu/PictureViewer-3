using System;
using System.Windows;

namespace PictureViewer {
	public partial class AskForTime : Window {
		public double time;
		private string type;

		public AskForTime(double defaultValue, string name, string type) {
			InitializeComponent();
			this.txtTime.Text = defaultValue.ToString();
			this.Title = "Change " + name + " Time";
			this.type = type;
		}

		private void BtnOK_Clicked(object sender, RoutedEventArgs e) {
			if (type.Equals("int")) {
				bool res = int.TryParse(this.txtTime.Text, out int newTime);
				if (res && newTime >= 0) {
					this.time = newTime;
					this.DialogResult = true;
					this.Close();
				} else {
					MessageBox.Show("Invalid time: " + this.txtTime.Text, "Invalid Time", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			} else {
				bool res = Double.TryParse(this.txtTime.Text, out double newTime);
				if (res && newTime >= 0) {
					this.time = newTime;
					this.DialogResult = true;
					this.Close();
				} else {
					MessageBox.Show("Invalid time: " + this.txtTime.Text, "Invalid Time", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
	}
}
