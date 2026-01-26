using System;
using System.Linq;
using System.Windows.Controls;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Controls;

public partial class AssetFormContent : UserControl
{
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }

    public AssetFormContent()
    {
        InitializeComponent();
        
        // Populate Equipment Type combo
        EquipmentTypeCombo.ItemsSource = Enum.GetValues(typeof(EquipmentType))
            .Cast<EquipmentType>()
            .Select(e => new ComboBoxItem { Content = e.ToString(), Tag = e })
            .ToList();
        EquipmentTypeCombo.SelectedIndex = 0;
    }

    public Asset GetAsset()
    {
        var selectedEquipmentType = (EquipmentTypeCombo.SelectedItem as ComboBoxItem)?.Tag as EquipmentType? ?? EquipmentType.Unknown;
        
        return new Asset
        {
            Serial = SerialTextBox.Text,
            Brand = BrandTextBox.Text,
            Model = ModelTextBox.Text,
            Nickname = NicknameTextBox.Text,
            EquipmentType = selectedEquipmentType,
            Notes = NotesTextBox.Text,
            CustomerId = CustomerId,
            SiteId = SiteId
        };
    }

    public void LoadAsset(Asset asset)
    {
        SerialTextBox.Text = asset.Serial;
        BrandTextBox.Text = asset.Brand;
        ModelTextBox.Text = asset.Model;
        NicknameTextBox.Text = asset.Nickname;
        NotesTextBox.Text = asset.Notes;
        CustomerId = asset.CustomerId;
        SiteId = asset.SiteId;
        
        // Select equipment type
        var item = EquipmentTypeCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (EquipmentType)i.Tag == asset.EquipmentType);
        if (item != null)
        {
            EquipmentTypeCombo.SelectedItem = item;
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(SerialTextBox.Text))
        {
            return false;
        }
        return true;
    }
}
