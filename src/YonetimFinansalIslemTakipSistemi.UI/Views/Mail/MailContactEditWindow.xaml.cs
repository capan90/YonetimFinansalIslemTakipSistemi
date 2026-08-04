using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Mail;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Mail;

public partial class MailContactEditWindow : Window
{
    private readonly MailContactEditViewModel _vm;

    /// <summary>Yeni kişi kaydedildiyse dolu — hızlı ekleme akışı otomatik seçim için kullanır.</summary>
    public MailContactDto? SavedContact => _vm.SavedContact;

    public MailContactEditWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm         = services.GetRequiredService<MailContactEditViewModel>();
        DataContext = _vm;

        _vm.SaveCompleted = () =>
        {
            DialogResult = true;
            Close();
        };
    }

    public void InitializeForEdit(MailContactDto dto) => _vm.InitializeForEdit(dto);

    /// <summary>Mail ekranından "rehbere ekle" ile gelindiğinde adres alanını ön doldurur.</summary>
    public void PrefillEmail(string email) => _vm.PrefillEmail(email);
}
