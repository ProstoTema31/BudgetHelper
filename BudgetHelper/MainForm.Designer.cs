namespace BudgetHelper
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnSelectSecond = new System.Windows.Forms.Button();
            this.btnSelectFirst = new System.Windows.Forms.Button();
            this.txtSecondFile = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtFirstFile = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnCompare = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.txtResult = new System.Windows.Forms.RichTextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnExport = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnSelectSecond);
            this.panel1.Controls.Add(this.btnSelectFirst);
            this.panel1.Controls.Add(this.txtSecondFile);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.txtFirstFile);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(784, 109);
            this.panel1.TabIndex = 0;
            // 
            // btnSelectSecond
            // 
            this.btnSelectSecond.BackColor = System.Drawing.Color.PaleGreen;
            this.btnSelectSecond.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSelectSecond.FlatAppearance.BorderSize = 0;
            this.btnSelectSecond.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectSecond.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSelectSecond.ForeColor = System.Drawing.Color.ForestGreen;
            this.btnSelectSecond.Location = new System.Drawing.Point(629, 58);
            this.btnSelectSecond.Name = "btnSelectSecond";
            this.btnSelectSecond.Size = new System.Drawing.Size(143, 33);
            this.btnSelectSecond.TabIndex = 5;
            this.btnSelectSecond.Text = "Выбрать";
            this.btnSelectSecond.UseVisualStyleBackColor = false;
            // 
            // btnSelectFirst
            // 
            this.btnSelectFirst.BackColor = System.Drawing.Color.PaleGreen;
            this.btnSelectFirst.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSelectFirst.FlatAppearance.BorderSize = 0;
            this.btnSelectFirst.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectFirst.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSelectFirst.ForeColor = System.Drawing.Color.ForestGreen;
            this.btnSelectFirst.Location = new System.Drawing.Point(629, 12);
            this.btnSelectFirst.Name = "btnSelectFirst";
            this.btnSelectFirst.Size = new System.Drawing.Size(143, 33);
            this.btnSelectFirst.TabIndex = 4;
            this.btnSelectFirst.Text = "Выбрать";
            this.btnSelectFirst.UseVisualStyleBackColor = false;
            // 
            // txtSecondFile
            // 
            this.txtSecondFile.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtSecondFile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSecondFile.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.txtSecondFile.Location = new System.Drawing.Point(138, 59);
            this.txtSecondFile.Name = "txtSecondFile";
            this.txtSecondFile.ReadOnly = true;
            this.txtSecondFile.Size = new System.Drawing.Size(473, 32);
            this.txtSecondFile.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.label2.Location = new System.Drawing.Point(15, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 25);
            this.label2.TabIndex = 2;
            this.label2.Text = "Второй акт:";
            // 
            // txtFirstFile
            // 
            this.txtFirstFile.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtFirstFile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFirstFile.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.txtFirstFile.Location = new System.Drawing.Point(138, 13);
            this.txtFirstFile.Name = "txtFirstFile";
            this.txtFirstFile.ReadOnly = true;
            this.txtFirstFile.Size = new System.Drawing.Size(473, 32);
            this.txtFirstFile.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.label1.Location = new System.Drawing.Point(15, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Первый акт:";
            // 
            // btnCompare
            // 
            this.btnCompare.BackColor = System.Drawing.Color.PaleTurquoise;
            this.btnCompare.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCompare.FlatAppearance.BorderSize = 0;
            this.btnCompare.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCompare.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.btnCompare.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.btnCompare.Location = new System.Drawing.Point(138, 125);
            this.btnCompare.Name = "btnCompare";
            this.btnCompare.Size = new System.Drawing.Size(473, 43);
            this.btnCompare.TabIndex = 6;
            this.btnCompare.Text = "Сравнить акты";
            this.btnCompare.UseVisualStyleBackColor = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(20, 187);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(752, 30);
            this.progressBar.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.label3.Location = new System.Drawing.Point(7, 238);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(165, 25);
            this.label3.TabIndex = 8;
            this.label3.Text = "Результат сверки:";
            // 
            // txtResult
            // 
            this.txtResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtResult.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtResult.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtResult.Location = new System.Drawing.Point(12, 266);
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.Size = new System.Drawing.Size(760, 283);
            this.txtResult.TabIndex = 9;
            this.txtResult.Text = "";
            // 
            // panel2
            // 
            this.panel2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.panel2.Controls.Add(this.btnExport);
            this.panel2.Controls.Add(this.progressBar);
            this.panel2.Controls.Add(this.btnCompare);
            this.panel2.Controls.Add(this.panel1);
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(784, 235);
            this.panel2.TabIndex = 10;
            this.panel2.Resize += new System.EventHandler(this.panel2_Resize);
            // 
            // btnExport
            // 
            this.btnExport.BackColor = System.Drawing.Color.LightSalmon;
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnExport.ForeColor = System.Drawing.Color.SaddleBrown;
            this.btnExport.Location = new System.Drawing.Point(629, 125);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(143, 43);
            this.btnExport.TabIndex = 8;
            this.btnExport.Text = "Экспорт";
            this.btnExport.UseVisualStyleBackColor = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.panel2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.Text = "Главный экран";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox txtFirstFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtSecondFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnSelectSecond;
        private System.Windows.Forms.Button btnSelectFirst;
        private System.Windows.Forms.Button btnCompare;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox txtResult;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnExport;
    }
}

