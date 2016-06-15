namespace ApplicationLogger {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.connectedToIPCLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.debugLogTextBox = new System.Windows.Forms.TextBox();
            this.labelApplication = new System.Windows.Forms.Label();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.focusDebug = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.focusDebug);
            this.groupBox1.Controls.Add(this.connectedToIPCLabel);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.debugLogTextBox);
            this.groupBox1.Controls.Add(this.labelApplication);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(702, 241);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Application information";
            // 
            // connectedToIPCLabel
            // 
            this.connectedToIPCLabel.AutoSize = true;
            this.connectedToIPCLabel.Location = new System.Drawing.Point(8, 35);
            this.connectedToIPCLabel.Name = "connectedToIPCLabel";
            this.connectedToIPCLabel.Size = new System.Drawing.Size(97, 13);
            this.connectedToIPCLabel.TabIndex = 3;
            this.connectedToIPCLabel.Text = "Connected to IPC: ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Log";
            // 
            // debugLogTextBox
            // 
            this.debugLogTextBox.Location = new System.Drawing.Point(11, 64);
            this.debugLogTextBox.Multiline = true;
            this.debugLogTextBox.Name = "debugLogTextBox";
            this.debugLogTextBox.ReadOnly = true;
            this.debugLogTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.debugLogTextBox.Size = new System.Drawing.Size(685, 171);
            this.debugLogTextBox.TabIndex = 1;
            // 
            // labelApplication
            // 
            this.labelApplication.AutoSize = true;
            this.labelApplication.Location = new System.Drawing.Point(8, 22);
            this.labelApplication.Name = "labelApplication";
            this.labelApplication.Size = new System.Drawing.Size(99, 13);
            this.labelApplication.TabIndex = 0;
            this.labelApplication.Text = "Current Application:";
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.onDoubleClickNotificationIcon);
            // 
            // focusDebug
            // 
            this.focusDebug.AutoSize = true;
            this.focusDebug.Location = new System.Drawing.Point(319, 35);
            this.focusDebug.Name = "focusDebug";
            this.focusDebug.Size = new System.Drawing.Size(98, 13);
            this.focusDebug.TabIndex = 4;
            this.focusDebug.Text = "Currently Focused: ";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(726, 265);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "Application Logger";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.onFormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.onFormClosed);
            this.Load += new System.EventHandler(this.onFormLoad);
            this.Resize += new System.EventHandler(this.onResize);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label labelApplication;
		private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.TextBox debugLogTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label connectedToIPCLabel;
        private System.Windows.Forms.Label focusDebug;
	}
}

