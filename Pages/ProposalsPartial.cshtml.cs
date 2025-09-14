using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace MultiplicationGame.Pages
{
    public class ProposalsPartialModel : PageModel
    {
        [BindProperty]
        public List<int?>? Answers { get; set; }
        public List<int> Proposals { get; set; } = new();

        [BindProperty]
        public List<int> ProposalsState { get; set; } = new();
        [BindProperty]
        public int? LastFilledIndex { get; set; }
        [BindProperty]
        public int? LastFilledValue { get; set; }

        public IActionResult OnPost()
        {
            // Propozycje pobieramy z hidden inputów
            Proposals = ProposalsState != null ? new List<int>(ProposalsState) : new List<int>();
            // Usuwamy tylko użyty kafelek (jeśli taki był)
            if (LastFilledValue.HasValue && Proposals.Contains(LastFilledValue.Value))
            {
                Proposals.Remove(LastFilledValue.Value);
            }
            return Partial("~/Pages/ProposalsPartial.cshtml", Proposals);
        }
    }
}
