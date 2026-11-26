using CommunityToolkit.Mvvm.ComponentModel;

namespace SalesDistricts.Client.ViewModels;

// One row in the district list. PrimarySalesperson can change in place, so the row keeps its selection.
public sealed partial class DistrictItemViewModel : ObservableObject
{
    public DistrictItemViewModel(int id, string name, string primarySalesperson)
    {
        Id = id;
        Name = name;
        PrimarySalesperson = primarySalesperson;
    }

    public int Id { get; }
    public string Name { get; }

    [ObservableProperty]
    public partial string PrimarySalesperson { get; set; }
}
