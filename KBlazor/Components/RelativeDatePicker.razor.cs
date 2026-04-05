using Microsoft.AspNetCore.Components;
using KBlazor.Models;

namespace KBlazor.Components
{
    public partial class RelativeDatePicker
    {
        protected string relativeChoice = string.Empty;
        protected int? xNumber = null;
        protected string datePart = string.Empty;

        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>The resolved lower bound of the selected relative date expression.</summary>
        [Parameter]
        public DateTime? StartDate { get; set; }

        [Parameter]
        public EventCallback<DateTime?> StartDateChanged { get; set; }

        /// <summary>The resolved upper bound of the selected relative date expression.</summary>
        [Parameter]
        public DateTime? EndDate { get; set; }

        [Parameter]
        public EventCallback<DateTime?> EndDateChanged { get; set; }

        /// <summary>Resolves the current Value via RelativeDateCalc and fires StartDateChanged / EndDateChanged.</summary>
        private async Task ResolveDates()
        {
            var val = Value;
            if (RelativeDateCalc.IsValidRelativeDate(val))
            {
                StartDate = RelativeDateCalc.GetLowerDate(val);
                EndDate = RelativeDateCalc.GetUpperDate(val);
            }
            else
            {
                StartDate = null;
                EndDate = null;
            }
            await StartDateChanged.InvokeAsync(StartDate);
            await EndDateChanged.InvokeAsync(EndDate);
        }

        [Parameter]
        public string Value
        {
            get
            {
                return $"{relativeChoice}{(xNumber.HasValue ? $" {xNumber.Value} " : " ")}{datePart}".Trim();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    switch (parts.FirstOrDefault())
                    {
                        case "Today":
                        case "This":
                        case "Last":
                        case "Next":
                            relativeChoice = parts.FirstOrDefault();
                            switch (parts.LastOrDefault())
                            {
                                case "Week":
                                case "Month":
                                case "Year":
                                    datePart = parts.Last();
                                    break;
                                default:
                                    datePart = string.Empty;
                                    break;
                            }
                            var middle = parts.Skip(1).FirstOrDefault();
                            if (!string.IsNullOrEmpty(middle))
                            {
                                int middleInt = 0;
                                if(int.TryParse(middle, out middleInt))
                                    xNumber = middleInt;
                                else
                                    xNumber = null;
                            }
                            break;
                        default:
                            relativeChoice = string.Empty;
                            break;
                    }
                }
                else
                {
                    relativeChoice = string.Empty;
                    xNumber = null;
                    datePart = string.Empty;
                }
            }
        }
    }
}
