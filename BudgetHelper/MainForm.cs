using BudgetHelper.Comparer;
using BudgetHelper.Models;
using BudgetHelper.Parsers;
using BudgetHelper.Storage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BudgetHelper
{
    public partial class MainForm : Form
    {
        private string firstFilePath;
        private string secondFilePath;

        // Имена JSON-файлов для временного хранения
        private const string Act1Json = "act1.json";
        private const string Act2Json = "act2.json";

        public MainForm()
        {
            InitializeComponent();

            btnSelectFirst.Click += BtnSelectFirst_Click;
            btnSelectSecond.Click += BtnSelectSecond_Click;
            btnCompare.Click += BtnCompare_Click;
        }

        private void BtnSelectFirst_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Допустимые файлы|*.xls;*.xlsx;*.pdf|Excel файлы|*.xls;*.xlsx|PDF файлы|*.pdf|Все файлы|*.*";
                ofd.Title = "Выберите первый акт";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    firstFilePath = ofd.FileName;
                    txtFirstFile.Text = Path.GetFileName(firstFilePath);
                    AppendColoredText($"[OK] Выбран первый файл: {Path.GetFileName(firstFilePath)}\n", Color.Green);
                }
            }
        }

        private void BtnSelectSecond_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Допустимые файлы|*.xls;*.xlsx;*.pdf|Excel файлы|*.xls;*.xlsx|PDF файлы|*.pdf|Все файлы|*.*";
                ofd.Title = "Выберите второй акт";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    secondFilePath = ofd.FileName;
                    txtSecondFile.Text = Path.GetFileName(secondFilePath);
                    AppendColoredText($"[OK] Выбран второй файл: {Path.GetFileName(secondFilePath)}\n", Color.Green);
                }
            }
        }

        private async void BtnCompare_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(firstFilePath) || string.IsNullOrEmpty(secondFilePath))
            {
                MessageBox.Show("Выберите оба файла!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnCompare.Enabled = false;
            progressBar.Visible = true;
            txtResult.Clear();

            try
            {
                AppendColoredText("[СТАРТ] Начинаю сверку...\n\n", Color.Blue);
                JsonStorageHelper.ClearStorage(Act1Json, Act2Json);

                AppendColoredText("[1/3] Парсинг первого файла...\n", Color.DarkBlue);
                var (header1, ops1) = await ParseFileAsync(firstFilePath, isSecondFile: false);
                AppendColoredText($"      Загружено операций: {ops1.Count}, начальное сальдо: {header1.OpeningBalance:N2}, конечное: {header1.ClosingBalance:N2}\n", Color.Green);
                JsonStorageHelper.SaveAct(Act1Json, header1, ops1);

                AppendColoredText("[2/3] Парсинг второго файла...\n", Color.DarkBlue);
                var (header2, ops2) = await ParseFileAsync(secondFilePath, isSecondFile: true);
                AppendColoredText($"      Загружено операций: {ops2.Count}, начальное сальдо: {header2.OpeningBalance:N2}, конечное: {header2.ClosingBalance:N2}\n", Color.Green);
                JsonStorageHelper.SaveAct(Act2Json, header2, ops2);

                AppendColoredText("[3/3] Загрузка из JSON и сравнение...\n\n", Color.DarkBlue);

                var (loadedHeader1, loadedOps1) = JsonStorageHelper.LoadAct(Act1Json);
                var (loadedHeader2, loadedOps2) = JsonStorageHelper.LoadAct(Act2Json);

                var comparer = new ActsComparer();
                var result = comparer.Compare(loadedOps1, loadedOps2,
                                              loadedHeader1.OpeningBalance, loadedHeader2.OpeningBalance,
                                              loadedHeader1.ClosingBalance, loadedHeader2.ClosingBalance);
                DisplayResult(result);
            }
            catch (Exception ex)
            {
                AppendColoredText($"\n[ОШИБКА] {ex.Message}\n", Color.Red);
                AppendColoredText(ex.StackTrace + "\n", Color.DarkGray);
            }
            finally
            {
                btnCompare.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private async Task<(DocumentHeader header, List<DocumentContent> operations)> ParseFileAsync(string path, bool isSecondFile)
        {
            return await Task.Run(() =>
            {
                if (path.EndsWith(".xls") || path.EndsWith(".xlsx"))
                    return ExcelParser.ParseExcel(path);
                else if (PdfParser.IsPdfFile(path))
                    return PdfParser.ParsePdf(path, extractCounterparty: isSecondFile);
                else
                    throw new NotSupportedException("Поддерживаются только Excel и PDF файлы");
            });
        }

        private void DisplayResult(ComparisonResult r)
        {
            AppendText(new string('=', 80) + "\n");
            AppendColoredText("РЕЗУЛЬТАТ СВЕРКИ\n", Color.Blue);
            AppendText(new string('=', 80) + "\n\n");

            AppendText($"Начальное сальдо: {r.OpeningBalanceFirst:N2} (файл 1) vs {r.OpeningBalanceSecond:N2} (файл 2)");
            if (r.OpeningBalancesMatch) AppendColoredText(" - СОВПАДАЕТ\n", Color.Green);
            else AppendColoredText(" - НЕ СОВПАДАЕТ\n", Color.Red);

            AppendText($"Конечное сальдо:   {r.ClosingBalanceFirst:N2} (файл 1) vs {r.ClosingBalanceSecond:N2} (файл 2)");
            if (r.ClosingBalancesMatch) AppendColoredText(" - СОВПАДАЕТ\n", Color.Green);
            else AppendColoredText(" - НЕ СОВПАДАЕТ\n", Color.Red);
            AppendText("\n");

            if (r.IsMatch)
            {
                AppendColoredText("[OK] АКТЫ СХОДЯТСЯ!\n", Color.Green);
                AppendText($"   Общая сумма операций (дебет-кредит): {r.ClosingBalanceFirst - r.OpeningBalanceFirst:N2} руб.\n");
                AppendText($"   Совпало операций: {r.MatchedOps} из {r.TotalFirstOps}\n");
                return;
            }

            AppendColoredText("[ОШИБКА] АКТЫ НЕ СХОДЯТСЯ!\n", Color.Red);
            AppendText($"   Расхождение по сумме операций: {r.TotalDifference:N2} руб.\n");
            AppendText($"   Операций в первом: {r.TotalFirstOps}, во втором: {r.TotalSecondOps}\n\n");

            if (r.Mismatches.Count > 0)
            {
                AppendColoredText($"РАСХОЖДЕНИЯ ({r.Mismatches.Count}):\n", Color.Orange);
                AppendText(new string('-', 80) + "\n");
                foreach (var m in r.Mismatches.Take(50))
                {
                    string line = $"   {m.Date,-12} {m.Amount,12:N2} руб.  {TruncateString(m.Description, 40)}";
                    if (!string.IsNullOrEmpty(m.Reason))
                        line += $"  [{m.Reason}]";
                    AppendColoredText(line + "\n", Color.Brown);
                }
                if (r.Mismatches.Count > 50)
                    AppendText($"   ... и еще {r.Mismatches.Count - 50}\n");
            }
        }

        private void AppendColoredText(string text, Color color)
        {
            if (txtResult.InvokeRequired)
            {
                txtResult.Invoke(new Action(() => AppendColoredText(text, color)));
                return;
            }
            txtResult.SelectionStart = txtResult.TextLength;
            txtResult.SelectionLength = 0;
            txtResult.SelectionColor = color;
            txtResult.AppendText(text);
            txtResult.SelectionColor = txtResult.ForeColor;
        }

        private void AppendText(string text)
        {
            if (txtResult.InvokeRequired)
            {
                txtResult.Invoke(new Action(() => AppendText(text)));
                return;
            }
            txtResult.AppendText(text);
        }

        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        private void panel2_Resize(object sender, EventArgs e)
        {
            panel1.Left = (this.ClientSize.Width - panel1.Width) / 2;
        }
    }
}