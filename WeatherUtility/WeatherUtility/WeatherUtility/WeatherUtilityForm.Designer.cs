using System.ComponentModel;
using System.Windows.Forms;

namespace WeatherUtility
{
    partial class WeatherUtilityForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose( );
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent( )
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WeatherUtilityForm));
            this.buttonDownload = new System.Windows.Forms.Button();
            this.groupBoxDownloadHistorical = new System.Windows.Forms.GroupBox();
            this.dateTimeEnd = new System.Windows.Forms.DateTimePicker();
            this.dateTimeStart = new System.Windows.Forms.DateTimePicker();
            this.labelEndDate = new System.Windows.Forms.Label();
            this.labelStartDate = new System.Windows.Forms.Label();
            this.progressStatusStrip = new System.Windows.Forms.StatusStrip();
            this.progressStatusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressDownloadBar = new System.Windows.Forms.ToolStripProgressBar();
            this.gridByInstallations = new System.Windows.Forms.DataGridView();
            this.tabShowSelection = new System.Windows.Forms.TabControl();
            this.tabPageByStation = new System.Windows.Forms.TabPage();
            this.gridByWeatherStations = new System.Windows.Forms.DataGridView();
            this.tabPageBySite = new System.Windows.Forms.TabPage();
            this.groupBoxDownloadHistorical.SuspendLayout();
            this.progressStatusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridByInstallations)).BeginInit();
            this.tabShowSelection.SuspendLayout();
            this.tabPageByStation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridByWeatherStations)).BeginInit();
            this.tabPageBySite.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonDownload
            // 
            this.buttonDownload.Location = new System.Drawing.Point(399, 28);
            this.buttonDownload.Name = "buttonDownload";
            this.buttonDownload.Size = new System.Drawing.Size(191, 52);
            this.buttonDownload.TabIndex = 0;
            this.buttonDownload.Text = "Download Weather Data";
            this.buttonDownload.UseVisualStyleBackColor = true;
            this.buttonDownload.Click += new System.EventHandler(this.buttonDownload_Click);
            // 
            // groupBoxDownloadHistorical
            // 
            this.groupBoxDownloadHistorical.Controls.Add(this.dateTimeEnd);
            this.groupBoxDownloadHistorical.Controls.Add(this.dateTimeStart);
            this.groupBoxDownloadHistorical.Controls.Add(this.labelEndDate);
            this.groupBoxDownloadHistorical.Controls.Add(this.labelStartDate);
            this.groupBoxDownloadHistorical.Controls.Add(this.buttonDownload);
            this.groupBoxDownloadHistorical.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxDownloadHistorical.Location = new System.Drawing.Point(0, 0);
            this.groupBoxDownloadHistorical.Name = "groupBoxDownloadHistorical";
            this.groupBoxDownloadHistorical.Size = new System.Drawing.Size(1000, 99);
            this.groupBoxDownloadHistorical.TabIndex = 2;
            this.groupBoxDownloadHistorical.TabStop = false;
            this.groupBoxDownloadHistorical.Text = "Download Historical Weather Data";
            // 
            // dateTimeEnd
            // 
            this.dateTimeEnd.Location = new System.Drawing.Point(110, 59);
            this.dateTimeEnd.Name = "dateTimeEnd";
            this.dateTimeEnd.Size = new System.Drawing.Size(252, 21);
            this.dateTimeEnd.TabIndex = 4;
            // 
            // dateTimeStart
            // 
            this.dateTimeStart.Location = new System.Drawing.Point(110, 28);
            this.dateTimeStart.MaxDate = new System.DateTime(2020, 12, 31, 0, 0, 0, 0);
            this.dateTimeStart.MinDate = new System.DateTime(2000, 1, 1, 0, 0, 0, 0);
            this.dateTimeStart.Name = "dateTimeStart";
            this.dateTimeStart.Size = new System.Drawing.Size(252, 21);
            this.dateTimeStart.TabIndex = 3;
            // 
            // labelEndDate
            // 
            this.labelEndDate.AutoSize = true;
            this.labelEndDate.Location = new System.Drawing.Point(30, 59);
            this.labelEndDate.Name = "labelEndDate";
            this.labelEndDate.Size = new System.Drawing.Size(56, 15);
            this.labelEndDate.TabIndex = 2;
            this.labelEndDate.Text = "End date";
            // 
            // labelStartDate
            // 
            this.labelStartDate.AutoSize = true;
            this.labelStartDate.Location = new System.Drawing.Point(30, 28);
            this.labelStartDate.Name = "labelStartDate";
            this.labelStartDate.Size = new System.Drawing.Size(59, 15);
            this.labelStartDate.TabIndex = 1;
            this.labelStartDate.Text = "Start date";
            // 
            // progressStatusStrip
            // 
            this.progressStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.progressStatusText,
            this.progressDownloadBar});
            this.progressStatusStrip.Location = new System.Drawing.Point(0, 708);
            this.progressStatusStrip.MinimumSize = new System.Drawing.Size(1000, 25);
            this.progressStatusStrip.Name = "progressStatusStrip";
            this.progressStatusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.progressStatusStrip.Size = new System.Drawing.Size(1000, 25);
            this.progressStatusStrip.TabIndex = 3;
            // 
            // progressStatusText
            // 
            this.progressStatusText.AutoSize = false;
            this.progressStatusText.AutoToolTip = true;
            this.progressStatusText.Name = "progressStatusText";
            this.progressStatusText.Size = new System.Drawing.Size(720, 20);
            this.progressStatusText.Text = "Status";
            this.progressStatusText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressDownloadBar
            // 
            this.progressDownloadBar.AutoSize = false;
            this.progressDownloadBar.Name = "progressDownloadBar";
            this.progressDownloadBar.Overflow = System.Windows.Forms.ToolStripItemOverflow.Always;
            this.progressDownloadBar.Size = new System.Drawing.Size(257, 19);
            this.progressDownloadBar.Step = 5;
            this.progressDownloadBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // gridByInstallations
            // 
            this.gridByInstallations.AllowUserToAddRows = false;
            this.gridByInstallations.AllowUserToDeleteRows = false;
            this.gridByInstallations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridByInstallations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridByInstallations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridByInstallations.Location = new System.Drawing.Point(3, 3);
            this.gridByInstallations.Name = "gridByInstallations";
            this.gridByInstallations.ShowCellErrors = false;
            this.gridByInstallations.ShowEditingIcon = false;
            this.gridByInstallations.ShowRowErrors = false;
            this.gridByInstallations.Size = new System.Drawing.Size(986, 575);
            this.gridByInstallations.TabIndex = 0;
            // 
            // tabShowSelection
            // 
            this.tabShowSelection.Controls.Add(this.tabPageByStation);
            this.tabShowSelection.Controls.Add(this.tabPageBySite);
            this.tabShowSelection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabShowSelection.Location = new System.Drawing.Point(0, 99);
            this.tabShowSelection.Name = "tabShowSelection";
            this.tabShowSelection.SelectedIndex = 0;
            this.tabShowSelection.Size = new System.Drawing.Size(1000, 609);
            this.tabShowSelection.TabIndex = 5;
            // 
            // tabPageByStation
            // 
            this.tabPageByStation.Controls.Add(this.gridByWeatherStations);
            this.tabPageByStation.Location = new System.Drawing.Point(4, 24);
            this.tabPageByStation.Name = "tabPageByStation";
            this.tabPageByStation.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageByStation.Size = new System.Drawing.Size(992, 581);
            this.tabPageByStation.TabIndex = 1;
            this.tabPageByStation.Text = "By Weather Station";
            this.tabPageByStation.UseVisualStyleBackColor = true;
            // 
            // gridByWeatherStations
            // 
            this.gridByWeatherStations.AllowUserToAddRows = false;
            this.gridByWeatherStations.AllowUserToDeleteRows = false;
            this.gridByWeatherStations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridByWeatherStations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridByWeatherStations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridByWeatherStations.Location = new System.Drawing.Point(3, 3);
            this.gridByWeatherStations.Name = "gridByWeatherStations";
            this.gridByWeatherStations.Size = new System.Drawing.Size(986, 575);
            this.gridByWeatherStations.TabIndex = 0;
            // 
            // tabPageBySite
            // 
            this.tabPageBySite.Controls.Add(this.gridByInstallations);
            this.tabPageBySite.Location = new System.Drawing.Point(4, 24);
            this.tabPageBySite.Name = "tabPageBySite";
            this.tabPageBySite.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageBySite.Size = new System.Drawing.Size(992, 581);
            this.tabPageBySite.TabIndex = 0;
            this.tabPageBySite.Text = "By Site";
            this.tabPageBySite.UseVisualStyleBackColor = true;
            // 
            // WeatherUtilityForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 733);
            this.Controls.Add(this.tabShowSelection);
            this.Controls.Add(this.groupBoxDownloadHistorical);
            this.Controls.Add(this.progressStatusStrip);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1016, 772);
            this.Name = "WeatherUtilityForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Weather Data - Download Utility";
            this.groupBoxDownloadHistorical.ResumeLayout(false);
            this.groupBoxDownloadHistorical.PerformLayout();
            this.progressStatusStrip.ResumeLayout(false);
            this.progressStatusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridByInstallations)).EndInit();
            this.tabShowSelection.ResumeLayout(false);
            this.tabPageByStation.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridByWeatherStations)).EndInit();
            this.tabPageBySite.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button buttonDownload;
        private GroupBox groupBoxDownloadHistorical;
        private DateTimePicker dateTimeEnd;
        private DateTimePicker dateTimeStart;
        private Label labelEndDate;
        private Label labelStartDate;
        private StatusStrip progressStatusStrip;
        private ToolStripStatusLabel progressStatusText;
        private ToolStripProgressBar progressDownloadBar;
        private DataGridView gridByInstallations;
        private TabControl tabShowSelection;
        private TabPage tabPageBySite;
        private TabPage tabPageByStation;
        private DataGridView gridByWeatherStations;
    }
}

