using Microsoft.AspNetCore.Components;

namespace KBlazor.Components
{
    public partial class CycleStateButton
    {
        [Parameter] public int State { get; set; }
        [Parameter] public EventCallback<int> OnStateChanged { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public string Tooltip { get; set; }
        private async Task CycleStateAsync()
        {
            State++;
            await OnStateChanged.InvokeAsync(State);
        }
    }
}
