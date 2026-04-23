using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Opc.Ua;
using OpcUaClientKit;

namespace OpcUaClientKit.WinFormsDemo;

public sealed class MainForm : Form
{
    private readonly IOpcUaClientFactory _factory = new OpcUaClientFactory();
    private readonly Label _connectionStatusLabel = new();
    private readonly TextBox _serverUrlTextBox = new() { Text = "opc.tcp://127.0.0.1:4840" };
    private readonly TextBox _applicationNameTextBox = new() { Text = "OpcUaClientKitWinFormsDemo" };
    private readonly TextBox _deviceIdTextBox = new() { Text = "winforms-demo" };
    private readonly CheckBox _anonymousCheckBox = new() { Text = "Anonymous" };
    private readonly TextBox _userNameTextBox = new() { Text = "OpcUaClient" };
    private readonly TextBox _passwordTextBox = new() { Text = "123456", UseSystemPasswordChar = true };
    private readonly CheckBox _useSecurityCheckBox = new() { Text = "Use security", Checked = true };
    private readonly ComboBox _securityPolicyComboBox = CreateComboBox();
    private readonly ComboBox _securityModeComboBox = CreateComboBox();
    private readonly CheckBox _autoAcceptCheckBox = new() { Text = "Auto accept untrusted certificate", Checked = true };
    private readonly CheckBox _checkDomainCheckBox = new() { Text = "Check certificate domain" };
    private readonly NumericUpDown _minimumKeySizeNumber = new() { Minimum = 512, Maximum = 8192, Increment = 512, Value = 2048 };
    private readonly NumericUpDown _sessionTimeoutNumber = new() { Minimum = 1000, Maximum = 600000, Increment = 1000, Value = 60000 };
    private readonly NumericUpDown _operationTimeoutNumber = new() { Minimum = 1000, Maximum = 600000, Increment = 1000, Value = 30000 };
    private readonly CheckBox _reconnectEnabledCheckBox = new() { Text = "Enable reconnect", Checked = true };
    private readonly TextBox _reconnectMaxAttemptsTextBox = new() { Text = "-1" };
    private readonly CheckBox _reconnectImmediateCheckBox = new() { Text = "Reconnect immediately", Checked = true };
    private readonly NumericUpDown _reconnectInitialDelayNumber = new() { Minimum = 100, Maximum = 600000, Increment = 100, Value = 1000 };
    private readonly NumericUpDown _reconnectMaxDelayNumber = new() { Minimum = 100, Maximum = 600000, Increment = 100, Value = 10000 };
    private readonly NumericUpDown _reconnectBackoffNumber = new() { Minimum = 1, Maximum = 10, DecimalPlaces = 1, Increment = 1, Value = 2 };
    private readonly Button _connectButton = new() { Text = "Connect" };
    private readonly Button _disconnectButton = new() { Text = "Disconnect", Enabled = false };

    private readonly TextBox _singleNodeIdTextBox = new() { Text = "ns=6;s=MyLevel" };
    private readonly TextBox _singleValueTextBox = new() { Text = "1" };
    private readonly ComboBox _singleTypeComboBox = CreateTypeComboBox();
    private readonly TextBox _singleResultTextBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly DataGridView _batchGrid = CreateGrid();
    private readonly Button _singleReadButton = new() { Text = "Read" };
    private readonly Button _singleWriteButton = new() { Text = "Write" };
    private readonly Button _batchReadButton = new() { Text = "Batch Read" };
    private readonly Button _batchWriteButton = new() { Text = "Batch Write" };

    private readonly TextBox _methodObjectNodeTextBox = new() { Text = "ns=6;s=MyDevice" };
    private readonly TextBox _methodNodeTextBox = new() { Text = "ns=6;s=MyMethod" };
    private readonly DataGridView _methodInputGrid = CreateGrid();
    private readonly DataGridView _methodOutputGrid = CreateGrid();
    private readonly Button _callMethodButton = new() { Text = "Call Method" };

    private readonly TextBox _dataNodeTextBox = new() { Text = "ns=6;s=MyLevel" };
    private readonly TextBox _dataDisplayNameTextBox = new() { Text = "Level" };
    private readonly NumericUpDown _dataPublishingIntervalNumber = new() { Minimum = 50, Maximum = 60000, Increment = 50, Value = 250 };
    private readonly Button _createDataSubscriptionButton = new() { Text = "Create Data Subscription" };
    private readonly Button _addDataNodeButton = new() { Text = "Add Node" };
    private readonly Button _stopDataSubscriptionButton = new() { Text = "Stop Data Subscription" };
    private readonly DataGridView _dataNotificationGrid = CreateGrid();

    private readonly TextBox _eventSourceTextBox = new() { Text = "ns=6;s=MyObjectsFolder" };
    private readonly TextBox _eventSourceNameTextBox = new() { Text = "MyObjects" };
    private readonly NumericUpDown _eventPublishingIntervalNumber = new() { Minimum = 100, Maximum = 60000, Increment = 100, Value = 500 };
    private readonly CheckBox _conditionRefreshOnStartCheckBox = new() { Text = "ConditionRefresh on start", Checked = true };
    private readonly CheckBox _ignoreSuppressedCheckBox = new() { Text = "Ignore suppressed or shelved" };
    private readonly ComboBox _selectClauseModeComboBox = CreateComboBox();
    private readonly Button _createEventSubscriptionButton = new() { Text = "Create Event Subscription" };
    private readonly Button _addEventSourceButton = new() { Text = "Add Source" };
    private readonly Button _refreshEventButton = new() { Text = "ConditionRefresh" };
    private readonly Button _stopEventSubscriptionButton = new() { Text = "Stop Event Subscription" };
    private readonly DataGridView _eventGrid = CreateGrid();
    private readonly DataGridView _eventFieldsGrid = CreateGrid();
    private readonly TextBox _logTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false
    };

    private IOpcUaClient? _client;
    private IOpcUaSubscription? _dataSubscription;
    private IOpcUaEventSubscription? _eventSubscription;
    private bool _isClosing;

    public MainForm()
    {
        Text = "OpcUaClientKit WinForms Demo (.NET Framework 4.6.2)";
        Width = 1250;
        Height = 850;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeOptions();
        InitializeUi();
        WireEvents();
        UpdateConnectionState();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            _isClosing = true;
            await CleanupAsync().ConfigureAwait(true);
            Close();
            return;
        }

        base.OnFormClosing(e);
    }

    private void InitializeOptions()
    {
        _securityPolicyComboBox.Items.AddRange(new object[]
        {
            string.Empty,
            "http://opcfoundation.org/UA/SecurityPolicy#Basic128Rsa15",
            "http://opcfoundation.org/UA/SecurityPolicy#Basic256",
            "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256",
            "http://opcfoundation.org/UA/SecurityPolicy#Aes128_Sha256_RsaOaep",
            "http://opcfoundation.org/UA/SecurityPolicy#Aes256_Sha256_RsaPss",
            "http://opcfoundation.org/UA/SecurityPolicy#None"
        });
        _securityPolicyComboBox.SelectedIndex = 0;

        _securityModeComboBox.Items.AddRange(new object[]
        {
            string.Empty,
            nameof(MessageSecurityMode.None),
            nameof(MessageSecurityMode.Sign),
            nameof(MessageSecurityMode.SignAndEncrypt)
        });
        _securityModeComboBox.SelectedIndex = 0;

        _selectClauseModeComboBox.Items.AddRange(Enum.GetNames(typeof(OpcUaEventSelectClauseMode)));
        _selectClauseModeComboBox.SelectedItem = nameof(OpcUaEventSelectClauseMode.Dynamic);
        _anonymousCheckBox.CheckedChanged += (_, _) => UpdateIdentityFields();
        UpdateIdentityFields();
    }

    private void InitializeUi()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateConnectionTab());
        tabs.TabPages.Add(CreateReadWriteTab());
        tabs.TabPages.Add(CreateMethodCallTab());
        tabs.TabPages.Add(CreateDataSubscriptionTab());
        tabs.TabPages.Add(CreateEventSubscriptionTab());
        tabs.TabPages.Add(CreateLogTab());

        var statusStrip = new StatusStrip();
        var statusItem = new ToolStripControlHost(_connectionStatusLabel);
        statusStrip.Items.Add(statusItem);

        Controls.Add(tabs);
        Controls.Add(statusStrip);
    }

    private TabPage CreateConnectionTab()
    {
        var page = new TabPage("Connection");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 15,
            Padding = new Padding(12),
            AutoScroll = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddRow(panel, 0, "Server URL", _serverUrlTextBox, "Application Name", _applicationNameTextBox);
        AddRow(panel, 1, "Device ID", _deviceIdTextBox, "Identity", _anonymousCheckBox);
        AddRow(panel, 2, "User Name", _userNameTextBox, "Password", _passwordTextBox);
        AddRow(panel, 3, "Use Security", _useSecurityCheckBox, "Check Domain", _checkDomainCheckBox);
        AddRow(panel, 4, "Security Policy", _securityPolicyComboBox, "Security Mode", _securityModeComboBox);
        AddRow(panel, 5, "Certificate", _autoAcceptCheckBox, "Minimum Key Size", _minimumKeySizeNumber);
        AddRow(panel, 6, "Session Timeout", _sessionTimeoutNumber, "Operation Timeout", _operationTimeoutNumber);
        AddRow(panel, 7, "Reconnect", _reconnectEnabledCheckBox, "Max Attempts", _reconnectMaxAttemptsTextBox);
        AddRow(panel, 8, "First Attempt", _reconnectImmediateCheckBox, "Initial Delay", _reconnectInitialDelayNumber);
        AddRow(panel, 9, "Max Delay", _reconnectMaxDelayNumber, "Backoff", _reconnectBackoffNumber);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_connectButton);
        buttons.Controls.Add(_disconnectButton);
        panel.Controls.Add(buttons, 1, 11);
        panel.SetColumnSpan(buttons, 3);

        page.Controls.Add(panel);
        return page;
    }

    private TabPage CreateReadWriteTab()
    {
        var page = new TabPage("Read / Write");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterDistance = 220 };

        var singlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            Padding = new Padding(12)
        };
        singlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        singlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        singlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        singlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddRow(singlePanel, 0, "NodeId", _singleNodeIdTextBox, "Type", _singleTypeComboBox);
        AddRow(singlePanel, 1, "Value", _singleValueTextBox, "Result", _singleResultTextBox);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        buttons.Controls.Add(_singleReadButton);
        buttons.Controls.Add(_singleWriteButton);
        singlePanel.Controls.Add(buttons, 1, 3);
        singlePanel.SetColumnSpan(buttons, 3);
        split.Panel1.Controls.Add(singlePanel);

        AddTextColumn(_batchGrid, "NodeId", "NodeId", 320);
        AddComboColumn(_batchGrid, "Type", "Type");
        AddTextColumn(_batchGrid, "Value", "Value", 180);
        AddTextColumn(_batchGrid, "Result", "Result", 250);
        AddTextColumn(_batchGrid, "Status", "Status", 220);
        _batchGrid.Rows.Add("ns=6;s=MyLevel", "Double", string.Empty, string.Empty, string.Empty);
        _batchGrid.Rows.Add("ns=3;s=/Plc/DB66.DBW0", "Int16", "1", string.Empty, string.Empty);
        _batchGrid.Rows.Add("ns=3;s=/Plc/DB66.DBW2", "Int16", "3", string.Empty, string.Empty);

        var batchPanel = new Panel { Dock = DockStyle.Fill };
        var batchButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36 };
        batchButtons.Controls.Add(_batchReadButton);
        batchButtons.Controls.Add(_batchWriteButton);
        batchPanel.Controls.Add(_batchGrid);
        batchPanel.Controls.Add(batchButtons);
        split.Panel2.Controls.Add(batchPanel);

        page.Controls.Add(split);
        return page;
    }

    private TabPage CreateMethodCallTab()
    {
        var page = new TabPage("Method Call");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = System.Windows.Forms.Orientation.Horizontal,
            SplitterDistance = 350
        };

        var inputPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 92, ColumnCount = 4 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddRow(top, 0, "Object NodeId", _methodObjectNodeTextBox, "Method NodeId", _methodNodeTextBox);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        buttons.Controls.Add(_callMethodButton);
        top.Controls.Add(buttons, 1, 1);
        top.SetColumnSpan(buttons, 3);

        AddComboColumn(_methodInputGrid, "Type", "Input Type");
        AddTextColumn(_methodInputGrid, "Value", "Input Value", 360);
        AddTextColumn(_methodInputGrid, "Status", "Status", 280);

        inputPanel.Controls.Add(_methodInputGrid);
        inputPanel.Controls.Add(top);

        AddTextColumn(_methodOutputGrid, "Index", "Index", 70);
        AddTextColumn(_methodOutputGrid, "Value", "Output Value", 520);
        AddTextColumn(_methodOutputGrid, "Type", "Output Type", 180);
        ConfigureStatusGrid(_methodOutputGrid);

        split.Panel1.Controls.Add(inputPanel);
        split.Panel2.Controls.Add(_methodOutputGrid);
        page.Controls.Add(split);
        return page;
    }

    private TabPage CreateDataSubscriptionTab()
    {
        var page = new TabPage("Data Subscription");
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 120, ColumnCount = 6 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        top.Controls.Add(new Label { Text = "NodeId", Dock = DockStyle.Fill }, 0, 0);
        top.Controls.Add(_dataNodeTextBox, 1, 0);
        top.Controls.Add(new Label { Text = "Display Name", Dock = DockStyle.Fill }, 2, 0);
        top.Controls.Add(_dataDisplayNameTextBox, 3, 0);
        top.Controls.Add(new Label { Text = "Publishing(ms)", Dock = DockStyle.Fill }, 4, 0);
        top.Controls.Add(_dataPublishingIntervalNumber, 5, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        buttons.Controls.Add(_createDataSubscriptionButton);
        buttons.Controls.Add(_addDataNodeButton);
        buttons.Controls.Add(_stopDataSubscriptionButton);
        top.Controls.Add(buttons, 1, 1);
        top.SetColumnSpan(buttons, 5);

        AddTextColumn(_dataNotificationGrid, "Time", "Time", 160);
        AddTextColumn(_dataNotificationGrid, "NodeId", "NodeId", 260);
        AddTextColumn(_dataNotificationGrid, "DisplayName", "DisplayName", 160);
        AddTextColumn(_dataNotificationGrid, "Value", "Value", 220);
        AddTextColumn(_dataNotificationGrid, "StatusCode", "StatusCode", 180);
        AddTextColumn(_dataNotificationGrid, "SourceTimestamp", "SourceTimestamp", 180);
        ConfigureStatusGrid(_dataNotificationGrid);

        panel.Controls.Add(_dataNotificationGrid);
        panel.Controls.Add(top);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage CreateEventSubscriptionTab()
    {
        var page = new TabPage("Alarm Events");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterDistance = 420 };
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 145, ColumnCount = 6 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        top.Controls.Add(new Label { Text = "Source NodeId", Dock = DockStyle.Fill }, 0, 0);
        top.Controls.Add(_eventSourceTextBox, 1, 0);
        top.Controls.Add(new Label { Text = "Display Name", Dock = DockStyle.Fill }, 2, 0);
        top.Controls.Add(_eventSourceNameTextBox, 3, 0);
        top.Controls.Add(new Label { Text = "Publishing(ms)", Dock = DockStyle.Fill }, 4, 0);
        top.Controls.Add(_eventPublishingIntervalNumber, 5, 0);
        top.Controls.Add(new Label { Text = "Select Clauses", Dock = DockStyle.Fill }, 0, 1);
        top.Controls.Add(_selectClauseModeComboBox, 1, 1);
        top.Controls.Add(_conditionRefreshOnStartCheckBox, 2, 1);
        top.SetColumnSpan(_conditionRefreshOnStartCheckBox, 2);
        top.Controls.Add(_ignoreSuppressedCheckBox, 4, 1);
        top.SetColumnSpan(_ignoreSuppressedCheckBox, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        buttons.Controls.Add(_createEventSubscriptionButton);
        buttons.Controls.Add(_addEventSourceButton);
        buttons.Controls.Add(_refreshEventButton);
        buttons.Controls.Add(_stopEventSubscriptionButton);
        top.Controls.Add(buttons, 1, 2);
        top.SetColumnSpan(buttons, 5);

        AddTextColumn(_eventGrid, "Time", "Time", 140);
        AddTextColumn(_eventGrid, "SourceName", "SourceName", 160);
        AddTextColumn(_eventGrid, "Message", "Message", 280);
        AddTextColumn(_eventGrid, "Severity", "Severity", 80);
        AddTextColumn(_eventGrid, "Active", "Active", 80);
        AddTextColumn(_eventGrid, "Acked", "Acked", 80);
        AddTextColumn(_eventGrid, "Retain", "Retain", 80);
        AddTextColumn(_eventGrid, "EventType", "EventType", 150);
        AddTextColumn(_eventGrid, "ConditionId", "ConditionId", 220);
        AddTextColumn(_eventGrid, "RowKey", "RowKey", 1);
        _eventGrid.Columns["RowKey"].Visible = false;
        ConfigureStatusGrid(_eventGrid);
        panel.Controls.Add(_eventGrid);
        panel.Controls.Add(top);

        AddTextColumn(_eventFieldsGrid, "Key", "Key", 280);
        AddTextColumn(_eventFieldsGrid, "DisplayName", "DisplayName", 220);
        AddTextColumn(_eventFieldsGrid, "Value", "Value", 500);
        ConfigureStatusGrid(_eventFieldsGrid);

        split.Panel1.Controls.Add(panel);
        split.Panel2.Controls.Add(_eventFieldsGrid);
        page.Controls.Add(split);
        return page;
    }

    private TabPage CreateLogTab()
    {
        var page = new TabPage("Log");
        page.Controls.Add(_logTextBox);
        return page;
    }

    private void WireEvents()
    {
        _connectButton.Click += async (_, _) => await RunUiAsync(_connectButton, ConnectAsync);
        _disconnectButton.Click += async (_, _) => await RunUiAsync(_disconnectButton, DisconnectAsync);
        _singleReadButton.Click += async (_, _) => await RunUiAsync(_singleReadButton, ReadSingleAsync);
        _singleWriteButton.Click += async (_, _) => await RunUiAsync(_singleWriteButton, WriteSingleAsync);
        _batchReadButton.Click += async (_, _) => await RunUiAsync(_batchReadButton, BatchReadAsync);
        _batchWriteButton.Click += async (_, _) => await RunUiAsync(_batchWriteButton, BatchWriteAsync);
        _callMethodButton.Click += async (_, _) => await RunUiAsync(_callMethodButton, ExecuteMethodAsync);
        _createDataSubscriptionButton.Click += async (_, _) => await RunUiAsync(_createDataSubscriptionButton, CreateDataSubscriptionAsync);
        _addDataNodeButton.Click += async (_, _) => await RunUiAsync(_addDataNodeButton, AddDataNodeAsync);
        _stopDataSubscriptionButton.Click += async (_, _) => await RunUiAsync(_stopDataSubscriptionButton, StopDataSubscriptionAsync);
        _createEventSubscriptionButton.Click += async (_, _) => await RunUiAsync(_createEventSubscriptionButton, CreateEventSubscriptionAsync);
        _addEventSourceButton.Click += async (_, _) => await RunUiAsync(_addEventSourceButton, AddEventSourceAsync);
        _refreshEventButton.Click += async (_, _) => await RunUiAsync(_refreshEventButton, RefreshEventSubscriptionAsync);
        _stopEventSubscriptionButton.Click += async (_, _) => await RunUiAsync(_stopEventSubscriptionButton, StopEventSubscriptionAsync);
    }

    private async Task ConnectAsync()
    {
        if (_client != null)
        {
            await DisconnectAsync().ConfigureAwait(true);
        }

        var builder = _factory
            .CreateBuilder()
            .WithServerUrl(_serverUrlTextBox.Text.Trim())
            .WithApplicationName(_applicationNameTextBox.Text.Trim())
            .WithDeviceId(_deviceIdTextBox.Text.Trim())
            .WithSecurity(_useSecurityCheckBox.Checked)
            .WithSessionTimeout((int)_sessionTimeoutNumber.Value)
            .WithOperationTimeout((int)_operationTimeoutNumber.Value)
            .WithCheckDomain(_checkDomainCheckBox.Checked)
            .WithAutoAcceptUntrustedServerCertificate(_autoAcceptCheckBox.Checked)
            .WithDiagnosticsHandler(diagnostic =>
            {
                AppendLog($"DIAGNOSTIC {diagnostic.Kind}: {diagnostic.Message}");
                if (diagnostic.Exception != null)
                {
                    AppendLog(diagnostic.Exception.Message);
                }
            })
            .WithCertificateOptions(options =>
            {
                options.MinimumKeySize = (ushort)_minimumKeySizeNumber.Value;
                options.RejectSHA1SignedCertificates = false;
            });

        if (_anonymousCheckBox.Checked)
        {
            builder.WithAnonymousIdentity();
        }
        else
        {
            builder.WithUserNamePassword(_userNameTextBox.Text.Trim(), _passwordTextBox.Text);
        }

        if (!string.IsNullOrWhiteSpace(_securityPolicyComboBox.Text))
        {
            builder.WithSecurityPolicyUri(_securityPolicyComboBox.Text.Trim());
        }

        var securityMode = ParseSecurityMode();
        if (securityMode.HasValue)
        {
            builder.WithMessageSecurityMode(securityMode.Value);
        }

        builder.WithReconnect(reconnect =>
        {
            reconnect.Enabled = _reconnectEnabledCheckBox.Checked;
            reconnect.MaxAttempts = ParseInt(_reconnectMaxAttemptsTextBox.Text, -1);
            reconnect.ReconnectImmediatelyOnFirstFailure = _reconnectImmediateCheckBox.Checked;
            reconnect.InitialDelayMs = (int)_reconnectInitialDelayNumber.Value;
            reconnect.MaxDelayMs = (int)_reconnectMaxDelayNumber.Value;
            reconnect.BackoffMultiplier = (double)_reconnectBackoffNumber.Value;
            reconnect.ReconnectHandler = OnReconnectEvent;
        });

        _client = builder.Build();
        await _client.ConnectAsync().ConfigureAwait(true);
        AppendLog("Connected.");
        UpdateConnectionState();
    }

    private async Task DisconnectAsync()
    {
        await StopEventSubscriptionAsync().ConfigureAwait(true);
        await StopDataSubscriptionAsync().ConfigureAwait(true);

        if (_client != null)
        {
            await _client.DisconnectAsync().ConfigureAwait(true);
            await DisposeAsync(_client).ConfigureAwait(true);
            _client = null;
        }

        AppendLog("Disconnected.");
        UpdateConnectionState();
    }

    private async Task ReadSingleAsync()
    {
        var client = GetClient();
        var value = await client.ReadNodeAsync(_singleNodeIdTextBox.Text.Trim()).ConfigureAwait(true);
        _singleResultTextBox.Text = FormatValue(value);
        AppendLog($"Read {_singleNodeIdTextBox.Text}: {FormatValue(value)}");
    }

    private async Task WriteSingleAsync()
    {
        var client = GetClient();
        var value = ConvertInputValue(_singleValueTextBox.Text, _singleTypeComboBox.Text);
        await client.WriteNodeAsync(_singleNodeIdTextBox.Text.Trim(), value).ConfigureAwait(true);
        AppendLog($"Wrote {_singleNodeIdTextBox.Text}: {FormatValue(value)}");
    }

    private async Task BatchReadAsync()
    {
        var client = GetClient();
        var rows = GetBatchRows(requireValue: false).ToList();
        foreach (var row in rows)
        {
            SetBatchStatus(row.Row, string.Empty, string.Empty);
        }

        try
        {
            var values = await client.ReadNodesAsync(rows.Select(row => row.NodeId)).ConfigureAwait(true);
            foreach (var row in rows)
            {
                values.TryGetValue(row.NodeId, out var value);
                SetBatchStatus(row.Row, FormatValue(value), "Good");
            }
        }
        catch (OpcUaBatchReadException ex)
        {
            foreach (var row in rows)
            {
                if (ex.SuccessfulValues.TryGetValue(row.NodeId, out var value))
                {
                    SetBatchStatus(row.Row, FormatValue(value), "Good");
                    continue;
                }

                var failure = ex.Failures.FirstOrDefault(item => item.NodeId == row.NodeId);
                SetBatchStatus(row.Row, string.Empty, failure?.Message ?? "Failed");
            }
        }
    }

    private async Task BatchWriteAsync()
    {
        var client = GetClient();
        var rows = GetBatchRows(requireValue: true).ToList();
        var values = new Dictionary<string, object?>();

        foreach (var row in rows)
        {
            SetBatchStatus(row.Row, string.Empty, string.Empty);
            if (values.ContainsKey(row.NodeId))
            {
                SetBatchStatus(row.Row, string.Empty, "Duplicate NodeId");
                continue;
            }

            values[row.NodeId] = ConvertInputValue(row.Value, row.TypeName);
        }

        try
        {
            await client.WriteNodesAsync(values).ConfigureAwait(true);
            foreach (var row in rows)
            {
                if (values.ContainsKey(row.NodeId))
                {
                    SetBatchStatus(row.Row, "Written", "Good");
                }
            }
        }
        catch (OpcUaBatchWriteException ex)
        {
            foreach (var row in rows)
            {
                if (ex.SuccessfulNodeIds.Contains(row.NodeId))
                {
                    SetBatchStatus(row.Row, "Written", "Good");
                    continue;
                }

                var failure = ex.Failures.FirstOrDefault(item => item.NodeId == row.NodeId);
                SetBatchStatus(row.Row, string.Empty, failure?.Message ?? "Failed");
            }
        }
    }

    private async Task ExecuteMethodAsync()
    {
        var objectNodeId = _methodObjectNodeTextBox.Text.Trim();
        var methodNodeId = _methodNodeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(objectNodeId) || string.IsNullOrWhiteSpace(methodNodeId))
        {
            throw new InvalidOperationException("Object NodeId and Method NodeId are required.");
        }

        var client = GetClient();
        var inputRows = GetMethodInputRows().ToList();
        var inputArguments = new List<object?>();

        foreach (var inputRow in inputRows)
        {
            SetMethodInputStatus(inputRow.Row, string.Empty);
            try
            {
                inputArguments.Add(ConvertInputValue(inputRow.Value, inputRow.TypeName));
            }
            catch (Exception ex)
            {
                SetMethodInputStatus(inputRow.Row, ex.Message);
                throw new InvalidOperationException($"Invalid method input value: {ex.Message}", ex);
            }
        }

        _methodOutputGrid.Rows.Clear();
        var outputs = await client
            .CallMethodAsync(objectNodeId, methodNodeId, inputArguments)
            .ConfigureAwait(true);

        if (outputs.Count == 0)
        {
            _methodOutputGrid.Rows.Add("-", "(no output arguments)", string.Empty);
        }
        else
        {
            for (var i = 0; i < outputs.Count; i++)
            {
                var output = outputs[i];
                _methodOutputGrid.Rows.Add(
                    i.ToString(CultureInfo.InvariantCulture),
                    FormatValue(output),
                    output?.GetType().Name ?? "(null)");
            }
        }

        AppendLog($"Called method {methodNodeId} on {objectNodeId}; inputs={inputArguments.Count}, outputs={outputs.Count}.");
    }

    private async Task CreateDataSubscriptionAsync()
    {
        var client = GetClient().AsSubscribable();
        await StopDataSubscriptionAsync().ConfigureAwait(true);
        _dataSubscription = await client
            .CreateSubscriptionBuilder()
            .WithName("winforms-data")
            .WithPublishingInterval((int)_dataPublishingIntervalNumber.Value)
            .BuildAsync()
            .ConfigureAwait(true);
        AppendLog("Data subscription created.");
    }

    private async Task AddDataNodeAsync()
    {
        if (_dataSubscription == null)
        {
            await CreateDataSubscriptionAsync().ConfigureAwait(true);
        }

        var node = new OpcUaNode(_dataNodeTextBox.Text.Trim(), _dataDisplayNameTextBox.Text.Trim());
        await _dataSubscription!.AddNodeAsync(node, OnDataChanged).ConfigureAwait(true);
        AppendLog($"Data node added: {node.NodeId}");
    }

    private async Task StopDataSubscriptionAsync()
    {
        if (_dataSubscription == null)
        {
            return;
        }

        await _dataSubscription.UnsubscribeAsync().ConfigureAwait(true);
        await DisposeAsync(_dataSubscription).ConfigureAwait(true);
        _dataSubscription = null;
        AppendLog("Data subscription stopped.");
    }

    private async Task CreateEventSubscriptionAsync()
    {
        var client = GetClient().AsEventSubscribable();
        await StopEventSubscriptionAsync().ConfigureAwait(true);
        _eventSubscription = await client
            .CreateEventSubscriptionBuilder()
            .WithName("winforms-events")
            .WithPublishingInterval((int)_eventPublishingIntervalNumber.Value)
            .WithConditionRefreshOnStart(_conditionRefreshOnStartCheckBox.Checked)
            .WithIgnoreSuppressedOrShelved(_ignoreSuppressedCheckBox.Checked)
            .WithSelectClauseMode(ParseSelectClauseMode())
            .BuildAsync(OnEventReceived)
            .ConfigureAwait(true);
        AppendLog("Event subscription created.");
    }

    private async Task AddEventSourceAsync()
    {
        if (_eventSubscription == null)
        {
            await CreateEventSubscriptionAsync().ConfigureAwait(true);
        }

        var node = new OpcUaNode(_eventSourceTextBox.Text.Trim(), _eventSourceNameTextBox.Text.Trim());
        await _eventSubscription!.AddSourceAsync(node).ConfigureAwait(true);
        AppendLog($"Event source added: {node.NodeId}");
    }

    private async Task RefreshEventSubscriptionAsync()
    {
        if (_eventSubscription == null)
        {
            throw new InvalidOperationException("Create an event subscription first.");
        }

        await _eventSubscription.RefreshAsync().ConfigureAwait(true);
        AppendLog("ConditionRefresh sent.");
    }

    private async Task StopEventSubscriptionAsync()
    {
        if (_eventSubscription == null)
        {
            return;
        }

        await _eventSubscription.UnsubscribeAsync().ConfigureAwait(true);
        await DisposeAsync(_eventSubscription).ConfigureAwait(true);
        _eventSubscription = null;
        AppendLog("Event subscription stopped.");
    }

    private void OnDataChanged(OpcUaValueChangeNotification notification)
    {
        Post(() =>
        {
            var row = FindRowByCellValue(_dataNotificationGrid, "NodeId", notification.NodeId) ??
                      AddStatusRow(_dataNotificationGrid);

            SetCellValues(
                row,
                ("Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)),
                ("NodeId", notification.NodeId),
                ("DisplayName", notification.DisplayName ?? string.Empty),
                ("Value", FormatValue(notification.Value)),
                ("StatusCode", notification.StatusCode),
                ("SourceTimestamp", notification.SourceTimestamp));

            FlashRow(row, System.Drawing.Color.FromArgb(222, 231, 236));
        });
    }

    private void OnEventReceived(OpcUaEventNotification notification)
    {
        Post(() =>
        {
            var rowKey = CreateEventRowKey(notification);
            var existingRow = FindRowByCellValue(_eventGrid, "RowKey", rowKey);
            if (ShouldRemoveAlarmFromCurrentList(notification))
            {
                if (existingRow != null)
                {
                    var wasSelected = existingRow.Selected;
                    _eventGrid.Rows.Remove(existingRow);
                    if (wasSelected)
                    {
                        _eventFieldsGrid.Rows.Clear();
                    }
                }

                AppendLog($"Alarm cleared: {notification.SourceName ?? notification.SourceNodeId} - {notification.Message}");
                return;
            }

            var row = existingRow ?? AddStatusRow(_eventGrid);

            SetCellValues(
                row,
                ("Time", notification.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                ("SourceName", notification.SourceName ?? string.Empty),
                ("Message", notification.Message ?? string.Empty),
                ("Severity", notification.Severity),
                ("Active", FormatNullableBool(notification.Active)),
                ("Acked", FormatNullableBool(notification.Acked)),
                ("Retain", FormatNullableBool(notification.Retain)),
                ("EventType", notification.EventTypeNodeId),
                ("ConditionId", notification.ConditionId ?? string.Empty),
                ("RowKey", rowKey));

            ApplyAlarmSeverityStyle(row, notification.Severity);
            row.Selected = true;
            _eventGrid.FirstDisplayedScrollingRowIndex = row.Index;

            _eventFieldsGrid.Rows.Clear();
            foreach (var field in notification.SelectedFields)
            {
                _eventFieldsGrid.Rows.Add(field.Key, field.DisplayName, FormatValue(field.Value));
            }
        });
    }

    private void OnReconnectEvent(OpcUaReconnectEvent reconnectEvent)
    {
        AppendLog(
            $"RECONNECT {reconnectEvent.Kind}, Attempt={reconnectEvent.AttemptNumber}, Next={reconnectEvent.NextRetryDelay}");
        if (reconnectEvent.Exception != null)
        {
            AppendLog(reconnectEvent.Exception.Message);
        }

        Post(UpdateConnectionState);
    }

    private async Task CleanupAsync()
    {
        try
        {
            await DisconnectAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppendLog($"Cleanup failed: {ex.Message}");
        }
    }

    private async Task RunUiAsync(Button button, Func<Task> action)
    {
        button.Enabled = false;
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex}");
            MessageBox.Show(this, ex.Message, "OPC UA Demo Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            button.Enabled = true;
            UpdateConnectionState();
        }
    }

    private IOpcUaClient GetClient()
    {
        if (_client == null || !_client.IsConnected)
        {
            throw new InvalidOperationException("Connect to an OPC UA server first.");
        }

        return _client;
    }

    private IEnumerable<BatchRow> GetBatchRows(bool requireValue)
    {
        foreach (DataGridViewRow row in _batchGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var nodeId = Convert.ToString(row.Cells["NodeId"].Value, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                continue;
            }

            var typeName = Convert.ToString(row.Cells["Type"].Value, CultureInfo.InvariantCulture);
            var value = Convert.ToString(row.Cells["Value"].Value, CultureInfo.InvariantCulture);
            if (requireValue && value == null)
            {
                SetBatchStatus(row, string.Empty, "Value is required");
                continue;
            }

            yield return new BatchRow(row, nodeId!, string.IsNullOrWhiteSpace(typeName) ? "String" : typeName!, value ?? string.Empty);
        }
    }

    private IEnumerable<MethodInputRow> GetMethodInputRows()
    {
        foreach (DataGridViewRow row in _methodInputGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var typeName = Convert.ToString(row.Cells["Type"].Value, CultureInfo.InvariantCulture);
            var value = Convert.ToString(row.Cells["Value"].Value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(typeName) && string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            yield return new MethodInputRow(
                row,
                string.IsNullOrWhiteSpace(typeName) ? "String" : typeName!,
                value ?? string.Empty);
        }
    }

    private void SetBatchStatus(DataGridViewRow row, string result, string status)
    {
        row.Cells["Result"].Value = result;
        row.Cells["Status"].Value = status;
    }

    private void SetMethodInputStatus(DataGridViewRow row, string status)
    {
        row.Cells["Status"].Value = status;
    }

    private object? ConvertInputValue(string text, string typeName)
    {
        if (string.Equals(typeName, "String", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        if (string.Equals(typeName, "Boolean", StringComparison.OrdinalIgnoreCase))
        {
            return bool.Parse(text);
        }

        if (string.Equals(typeName, "Int16", StringComparison.OrdinalIgnoreCase))
        {
            return short.Parse(text, CultureInfo.InvariantCulture);
        }

        if (string.Equals(typeName, "Int32", StringComparison.OrdinalIgnoreCase))
        {
            return int.Parse(text, CultureInfo.InvariantCulture);
        }

        if (string.Equals(typeName, "UInt16", StringComparison.OrdinalIgnoreCase))
        {
            return ushort.Parse(text, CultureInfo.InvariantCulture);
        }

        if (string.Equals(typeName, "UInt32", StringComparison.OrdinalIgnoreCase))
        {
            return uint.Parse(text, CultureInfo.InvariantCulture);
        }

        if (string.Equals(typeName, "Float", StringComparison.OrdinalIgnoreCase))
        {
            return float.Parse(text, CultureInfo.InvariantCulture);
        }

        if (string.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase))
        {
            return double.Parse(text, CultureInfo.InvariantCulture);
        }

        if (string.Equals(typeName, "DateTime", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.Parse(text, CultureInfo.InvariantCulture);
        }

        throw new InvalidOperationException($"Unsupported value type '{typeName}'.");
    }

    private MessageSecurityMode? ParseSecurityMode()
    {
        return Enum.TryParse<MessageSecurityMode>(_securityModeComboBox.Text, ignoreCase: true, out var mode)
            ? mode
            : null;
    }

    private OpcUaEventSelectClauseMode ParseSelectClauseMode()
    {
        return Enum.TryParse<OpcUaEventSelectClauseMode>(_selectClauseModeComboBox.Text, ignoreCase: true, out var mode)
            ? mode
            : OpcUaEventSelectClauseMode.Dynamic;
    }

    private int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private void UpdateIdentityFields()
    {
        _userNameTextBox.Enabled = !_anonymousCheckBox.Checked;
        _passwordTextBox.Enabled = !_anonymousCheckBox.Checked;
    }

    private void UpdateConnectionState()
    {
        var connected = _client?.IsConnected == true;
        _connectionStatusLabel.Text = connected ? "Connected" : "Disconnected";
        _connectButton.Enabled = !connected;
        _disconnectButton.Enabled = connected;
    }

    private void AppendLog(string message)
    {
        Post(() =>
        {
            _logTextBox.AppendText(
                $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        });
    }

    private void Post(Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }

    private static async Task DisposeAsync(IAsyncDisposable disposable)
    {
        await disposable.DisposeAsync().AsTask().ConfigureAwait(false);
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "(null)";
        }

        if (value is byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty;
    }

    private static string FormatNullableBool(bool? value)
    {
        return value.HasValue ? value.Value.ToString() : "?";
    }

    private static DataGridViewRow AddStatusRow(DataGridView grid)
    {
        var row = (DataGridViewRow)grid.RowTemplate.Clone();
        row.CreateCells(grid);
        grid.Rows.Insert(0, row);
        return grid.Rows[0];
    }

    private static DataGridViewRow? FindRowByCellValue(
        DataGridView grid,
        string columnName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var cellValue = Convert.ToString(row.Cells[columnName].Value, CultureInfo.InvariantCulture);
            if (string.Equals(cellValue, value, StringComparison.Ordinal))
            {
                return row;
            }
        }

        return null;
    }

    private static void SetCellValues(
        DataGridViewRow row,
        params (string ColumnName, object? Value)[] values)
    {
        foreach (var value in values)
        {
            row.Cells[value.ColumnName].Value = value.Value;
        }
    }

    private static string CreateEventRowKey(OpcUaEventNotification notification)
    {
        if (!string.IsNullOrWhiteSpace(notification.ConditionId) &&
            !string.Equals(notification.ConditionId, "i=0", StringComparison.Ordinal))
        {
            return notification.ConditionId!;
        }

        var source = notification.SourceNodeId ?? notification.SourceName ?? string.Empty;
        var message = notification.Message ?? string.Empty;
        return string.Join("|", source, notification.EventTypeNodeId, message);
    }

    private static bool ShouldRemoveAlarmFromCurrentList(OpcUaEventNotification notification)
    {
        // This demo grid represents current alarms. Cleared/inactive conditions are logged but removed.
        return notification.Retain == false || notification.Active == false;
    }

    private static void ApplyAlarmSeverityStyle(DataGridViewRow row, ushort severity)
    {
        var backColor = GetAlarmSeverityBackColor(severity);
        var foreColor = severity >= 800
            ? System.Drawing.Color.White
            : System.Drawing.Color.FromArgb(32, 32, 32);

        row.DefaultCellStyle.BackColor = backColor;
        row.DefaultCellStyle.ForeColor = foreColor;
        row.DefaultCellStyle.SelectionBackColor = Darken(backColor);
        row.DefaultCellStyle.SelectionForeColor = foreColor;
    }

    private static System.Drawing.Color GetAlarmSeverityBackColor(ushort severity)
    {
        if (severity >= 800)
        {
            return System.Drawing.Color.FromArgb(178, 34, 34);
        }

        if (severity >= 600)
        {
            return System.Drawing.Color.FromArgb(255, 128, 74);
        }

        if (severity >= 400)
        {
            return System.Drawing.Color.FromArgb(255, 213, 92);
        }

        if (severity >= 200)
        {
            return System.Drawing.Color.FromArgb(255, 245, 178);
        }

        return System.Drawing.Color.FromArgb(230, 236, 242);
    }

    private static System.Drawing.Color Darken(System.Drawing.Color color)
    {
        return System.Drawing.Color.FromArgb(
            Math.Max(0, color.R - 35),
            Math.Max(0, color.G - 35),
            Math.Max(0, color.B - 35));
    }

    private static void FlashRow(DataGridViewRow row, System.Drawing.Color color)
    {
        row.DefaultCellStyle.BackColor = color;
        row.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(42, 52, 56);
        row.DefaultCellStyle.SelectionBackColor = Darken(color);
        row.DefaultCellStyle.SelectionForeColor = row.DefaultCellStyle.ForeColor;
    }

    private static void TrimRows(DataGridView grid, int maxRows)
    {
        while (grid.Rows.Count > maxRows)
        {
            grid.Rows.RemoveAt(grid.Rows.Count - 1);
        }
    }

    private static ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Dock = DockStyle.Fill
        };
    }

    private static ComboBox CreateTypeComboBox()
    {
        var combo = CreateComboBox();
        combo.Items.AddRange(new object[]
        {
            "String",
            "Boolean",
            "Int16",
            "Int32",
            "UInt16",
            "UInt32",
            "Float",
            "Double",
            "DateTime"
        });
        combo.SelectedItem = "Double";
        return combo;
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
    }

    private static void ConfigureStatusGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
    }

    private static void AddTextColumn(DataGridView grid, string name, string headerText, int width)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = headerText,
            Width = width
        });
    }

    private static void AddComboColumn(DataGridView grid, string name, string headerText)
    {
        grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = name,
            HeaderText = headerText,
            Width = 120,
            DataSource = new[]
            {
                "String",
                "Boolean",
                "Int16",
                "Int32",
                "UInt16",
                "UInt32",
                "Float",
                "Double",
                "DateTime"
            }
        });
    }

    private static void AddRow(
        TableLayoutPanel panel,
        int row,
        string label1,
        Control control1,
        string label2,
        Control control2)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.Controls.Add(new Label { Text = label1, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, row);
        control1.Dock = DockStyle.Fill;
        panel.Controls.Add(control1, 1, row);
        panel.Controls.Add(new Label { Text = label2, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, row);
        control2.Dock = DockStyle.Fill;
        panel.Controls.Add(control2, 3, row);
    }

    private sealed class BatchRow
    {
        public BatchRow(DataGridViewRow row, string nodeId, string typeName, string value)
        {
            Row = row;
            NodeId = nodeId;
            TypeName = typeName;
            Value = value;
        }

        public DataGridViewRow Row { get; }

        public string NodeId { get; }

        public string TypeName { get; }

        public string Value { get; }
    }

    private sealed class MethodInputRow
    {
        public MethodInputRow(DataGridViewRow row, string typeName, string value)
        {
            Row = row;
            TypeName = typeName;
            Value = value;
        }

        public DataGridViewRow Row { get; }

        public string TypeName { get; }

        public string Value { get; }
    }
}
