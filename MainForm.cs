using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HesapTakip;

public class MainForm : Form
{
    private readonly string AppDir;
    private readonly string DbPath;

    private TabControl tabs = new();

    // Rates tab
    private TextBox txtUsd = new();
    private TextBox txtEur = new();
    private TextBox txtGbp = new();
    private Button btnSaveRates = new();
    private Label lblRatesStatus = new();

    // Track tab inputs
    private DateTimePicker dtDate = new();
    private TextBox txtDesc = new();
    private ComboBox cbFlow = new();
    private ComboBox cbCur = new();
    private TextBox txtAmtFx = new();
    private Label lblRate = new();
    private Label lblAmtTry = new();

    private Button btnAdd = new();
    private Button btnDelete = new();
    private Button btnExport = new();

    private DataGridView grid = new();
    private Label lblTotal = new();

    private readonly CultureInfo TR = new("tr-TR");

    public MainForm()
    {
        Text = "Hesap Takip";
        Width = 1050;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HesapTakip");
        Directory.CreateDirectory(AppDir);
        DbPath = Path.Combine(AppDir, "hesap_takip.db");

        InitDb();
        BuildUi();
        LoadRatesIntoUi();
        RefreshGridAndTotal();
        RecalcPreview();
    }

    private void BuildUi()
    {
        tabs.Dock = DockStyle.Fill;

        // TAB 1: KURLAR
        var tabRates = new TabPage("Kurlar");
        var pnlRates = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(16),
            AutoSize = true
        };
        pnlRates.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        pnlRates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        pnlRates.Controls.Add(new Label { Text = "USD Kur (TRY):", AutoSize = true }, 0, 0);
        pnlRates.Controls.Add(txtUsd, 1, 0);

        pnlRates.Controls.Add(new Label { Text = "EUR Kur (TRY):", AutoSize = true }, 0, 1);
        pnlRates.Controls.Add(txtEur, 1, 1);

        pnlRates.Controls.Add(new Label { Text = "GBP Kur (TRY):", AutoSize = true }, 0, 2);
        pnlRates.Controls.Add(txtGbp, 1, 2);

        btnSaveRates.Text = "Kurları Kaydet";
        btnSaveRates.Width = 140;
        btnSaveRates.Click += (_, __) => SaveRates();

        pnlRates.Controls.Add(btnSaveRates, 1, 3);
        pnlRates.Controls.Add(lblRatesStatus, 1, 4);

        tabRates.Controls.Add(pnlRates);

        // TAB 2: HESAP TAKİP
        var tabTrack = new TabPage("Hesap Takip");

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        var pnlInput = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 4,
            Padding = new Padding(16)
        };

        pnlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        pnlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        pnlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        pnlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        pnlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        pnlInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        dtDate.Format = DateTimePickerFormat.Short;

        cbFlow.DropDownStyle = ComboBoxStyle.DropDownList;
        cbFlow.Items.AddRange(new object[] { "Gelir", "Gider" });
        cbFlow.SelectedIndex = 0;

        cbCur.DropDownStyle = ComboBoxStyle.DropDownList;
        cbCur.Items.AddRange(new object[] { "TRY", "USD", "EUR", "GBP" });
        cbCur.SelectedIndex = 0;
        cbCur.SelectedIndexChanged += (_, __) => RecalcPreview();

        txtAmtFx.TextChanged += (_, __) => RecalcPreview();

        pnlInput.Controls.Add(new Label { Text = "Tarih:", AutoSize = true }, 0, 0);
        pnlInput.Controls.Add(dtDate, 1, 0);

        pnlInput.Controls.Add(new Label { Text = "Gelir/Gider:", AutoSize = true }, 2, 0);
        pnlInput.Controls.Add(cbFlow, 3, 0);

        pnlInput.Controls.Add(new Label { Text = "Döviz:", AutoSize = true }, 4, 0);
        pnlInput.Controls.Add(cbCur, 5, 0);

        pnlInput.Controls.Add(new Label { Text = "Açıklama:", AutoSize = true }, 0, 1);
        pnlInput.Controls.Add(txtDesc, 1, 1);
        pnlInput.SetColumnSpan(txtDesc, 5);

        pnlInput.Controls.Add(new Label { Text = "Tutar (Döviz):", AutoSize = true }, 0, 2);
        pnlInput.Controls.Add(txtAmtFx, 1, 2);

        pnlInput.Controls.Add(new Label { Text = "Kur (TRY):", AutoSize = true }, 2, 2);
        pnlInput.Controls.Add(lblRate, 3, 2);

        pnlInput.Controls.Add(new Label { Text = "Tutar (TRY):", AutoSize = true }, 4, 2);
        pnlInput.Controls.Add(lblAmtTry, 5, 2);

        btnAdd.Text = "Ekle";
        btnAdd.Click += (_, __) => AddTx();

        btnDelete.Text = "Seçileni Sil";
        btnDelete.Click += (_, __) => DeleteSelected();

        btnExport.Text = "CSV Dışa Aktar";
        btnExport.Click += (_, __) => ExportCsv();

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        btnRow.Controls.Add(btnAdd);
        btnRow.Controls.Add(btnDelete);
        btnRow.Controls.Add(btnExport);
        pnlInput.Controls.Add(btnRow, 1, 3);
        pnlInput.SetColumnSpan(btnRow, 5);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        lblTotal.Dock = DockStyle.Fill;
        lblTotal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        root.Controls.Add(pnlInput, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(lblTotal, 0, 2);

        tabTrack.Controls.Add(root);

        tabs.TabPages.Add(tabRates);
        tabs.TabPages.Add(tabTrack);

        Controls.Add(tabs);
    }

    // DB
    private SqliteConnection OpenConn()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        return conn;
    }

    private void InitDb()
    {
        using var conn = OpenConn();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS rates (
  currency TEXT PRIMARY KEY,
  rate_try REAL NOT NULL
);
CREATE TABLE IF NOT EXISTS transactions (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  tx_date TEXT NOT NULL,
  description TEXT,
  flow_type TEXT NOT NULL,
  currency TEXT NOT NULL,
  amount_fx REAL NOT NULL,
  rate_try REAL NOT NULL,
  amount_try REAL NOT NULL
);
";
        cmd.ExecuteNonQuery();

        foreach (var c in new[] { "USD", "EUR", "GBP" })
        {
            using var ins = conn.CreateCommand();
            ins.CommandText = "INSERT OR IGNORE INTO rates(currency, rate_try) VALUES ($c, $r)";
            ins.Parameters.AddWithValue("$c", c);
            ins.Parameters.AddWithValue("$r", 0.0);
            ins.ExecuteNonQuery();
        }
    }

    private double GetRate(string currency)
    {
        if (currency == "TRY") return 1.0;

        using var conn = OpenConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT rate_try FROM rates WHERE currency=$c";
        cmd.Parameters.AddWithValue("$c", currency);
        var obj = cmd.ExecuteScalar();
        return obj == null ? 0.0 : Convert.ToDouble(obj, TR);
    }

    private void UpsertRate(string currency, double rate)
    {
        using var conn = OpenConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO rates(currency, rate_try) VALUES ($c, $r)
ON CONFLICT(currency) DO UPDATE SET rate_try=excluded.rate_try";
        cmd.Parameters.AddWithValue("$c", currency);
        cmd.Parameters.AddWithValue("$r", rate);
        cmd.ExecuteNonQuery();
    }

    // RATES
    private void LoadRatesIntoUi()
    {
        txtUsd.Text = GetRate("USD").ToString(TR);
        txtEur.Text = GetRate("EUR").ToString(TR);
        txtGbp.Text = GetRate("GBP").ToString(TR);
    }

    private void SaveRates()
    {
        try
        {
            var usd = ParsePositive(txtUsd.Text, "USD");
            var eur = ParsePositive(txtEur.Text, "EUR");
            var gbp = ParsePositive(txtGbp.Text, "GBP");

            UpsertRate("USD", usd);
            UpsertRate("EUR", eur);
            UpsertRate("GBP", gbp);

            lblRatesStatus.Text = "Kaydedildi.";
            RecalcPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // TRACK
    private void RecalcPreview()
    {
        var cur = cbCur.SelectedItem?.ToString() ?? "TRY";
        var rate = GetRate(cur);
        lblRate.Text = cur == "TRY" ? "1,00" : rate.ToString("N4", TR);

        double amtFx = 0;
        if (!string.IsNullOrWhiteSpace(txtAmtFx.Text))
        {
            double.TryParse(NormalizeNumber(txtAmtFx.Text), NumberStyles.Any, CultureInfo.InvariantCulture, out amtFx);
        }

        var amtTry = amtFx * rate;
        lblAmtTry.Text = amtTry.ToString("N2", TR);
    }

    private void AddTx()
    {
        try
        {
            var txDate = dtDate.Value.ToString("yyyy-MM-dd");
            var desc = txtDesc.Text?.Trim() ?? "";
            var flow = cbFlow.SelectedItem?.ToString() ?? "Gelir";
            var cur = cbCur.SelectedItem?.ToString() ?? "TRY";

            var amtFx = ParsePositive(txtAmtFx.Text, "Tutar (Döviz)");
            var rate = GetRate(cur);
            if (cur != "TRY" && rate <= 0) throw new Exception($"{cur} kuru tanımlı değil. Kurlar sekmesinden gir.");

            var amtTry = amtFx * rate;

            using var conn = OpenConn();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO transactions(tx_date, description, flow_type, currency, amount_fx, rate_try, amount_try)
VALUES ($d, $desc, $flow, $cur, $fx, $rate, $try)";
            cmd.Parameters.AddWithValue("$d", txDate);
            cmd.Parameters.AddWithValue("$desc", desc);
            cmd.Parameters.AddWithValue("$flow", flow);
            cmd.Parameters.AddWithValue("$cur", cur);
            cmd.Parameters.AddWithValue("$fx", amtFx);
            cmd.Parameters.AddWithValue("$rate", rate);
            cmd.Parameters.AddWithValue("$try", amtTry);
            cmd.ExecuteNonQuery();

            txtDesc.Clear();
            txtAmtFx.Clear();
            cbCur.SelectedItem = "TRY";
            RefreshGridAndTotal();
            RecalcPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteSelected()
    {
        if (grid.CurrentRow == null) return;
        if (grid.CurrentRow.Cells["id"].Value == null) return;

        var id = Convert.ToInt64(grid.CurrentRow.Cells["id"].Value);
        using var conn = OpenConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM transactions WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        RefreshGridAndTotal();
    }

    private void RefreshGridAndTotal()
    {
        using var conn = OpenConn();

        var dt = new DataTable();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT
  id,
  tx_date as Tarih,
  description as Açıklama,
  flow_type as Tip,
  currency as Döviz,
  ROUND(amount_fx, 2) as [Tutar(FX)],
  CASE WHEN currency='TRY' THEN 1.0 ELSE ROUND(rate_try, 4) END as Kur,
  ROUND(amount_try, 2) as [Tutar(TRY)]
FROM transactions
ORDER BY tx_date DESC, id DESC";
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
        }

        grid.DataSource = dt;
        if (grid.Columns.Contains("id"))
            grid.Columns["id"].Visible = false;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT COALESCE(SUM(CASE WHEN flow_type='Gelir' THEN amount_try ELSE -amount_try END), 0) FROM transactions";
            var net = Convert.ToDouble(cmd.ExecuteScalar() ?? 0.0);
            lblTotal.Text = $"Toplam (TRY): {net.ToString("N2", TR)}";
        }
    }

    private void ExportCsv()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = "hesap_takip.csv"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        using var conn = OpenConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT tx_date, description, flow_type, currency, amount_fx, rate_try, amount_try
FROM transactions
ORDER BY tx_date DESC, id DESC";
        using var reader = cmd.ExecuteReader();

        var sb = new StringBuilder();
        sb.AppendLine("Tarih;Açıklama;Tip;Döviz;Tutar(FX);Kur;Tutar(TRY)");

        while (reader.Read())
        {
            var d = reader.GetString(0);
            var desc = reader.IsDBNull(1) ? "" : reader.GetString(1).Replace(";", " ");
            var flow = reader.GetString(2);
            var cur = reader.GetString(3);
            var fx = reader.GetDouble(4);
            var rate = reader.GetDouble(5);
            var tr = reader.GetDouble(6);

            sb.AppendLine($"{d};{desc};{flow};{cur};{fx.ToString("N2", TR)};{rate.ToString("N4", TR)};{tr.ToString("N2", TR)}");
        }

        File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        MessageBox.Show("CSV dışa aktarıldı.", "Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Helpers
    private double ParsePositive(string input, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new Exception($"{fieldName} boş olamaz.");

        var norm = NormalizeNumber(input);
        if (!double.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            throw new Exception($"{fieldName} sayı değil.");

        if (v <= 0) throw new Exception($"{fieldName} 0'dan büyük olmalı.");
        return v;
    }

    private string NormalizeNumber(string s)
    {
        s = s.Trim();
        s = s.Replace(" ", "");
        s = s.Replace(".", "");
        s = s.Replace(",", ".");
        return s;
    }
}
