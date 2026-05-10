using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.Services;

public class EmailNotificationService : INotificationService
{
    private readonly string _host;
    private readonly int _port;
    private readonly bool _useSsl;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _fromName;
    private readonly string _agencyName;
    private readonly string _agencySite;
    private readonly bool _isConfigured;

    public EmailNotificationService()
    {
        _host       = ConfigurationManager.AppSettings["Smtp:Host"]       ?? "";
        _port       = int.TryParse(ConfigurationManager.AppSettings["Smtp:Port"], out var p) ? p : 587;
        _useSsl     = ConfigurationManager.AppSettings["Smtp:UseSsl"]     != "false";
        _userName   = ConfigurationManager.AppSettings["Smtp:UserName"]   ?? "";
        _password   = ConfigurationManager.AppSettings["Smtp:Password"]   ?? "";
        _fromName   = ConfigurationManager.AppSettings["Smtp:FromName"]   ?? "Агентство недвижимости";
        _agencyName = ConfigurationManager.AppSettings["Smtp:AgencyName"] ?? "HomeEstate";
        _agencySite = ConfigurationManager.AppSettings["Smtp:AgencySite"] ?? "";

        Debug.WriteLine($"[Email] Config: host={_host} port={_port} user={_userName} passLen={_password.Length}");

        // Считаем сервис настроенным если введены реальные данные
        _isConfigured = !string.IsNullOrWhiteSpace(_host)
                     && !string.IsNullOrWhiteSpace(_userName)
                     && _userName != "your-email@gmail.com"
                     && !string.IsNullOrWhiteSpace(_password)
                     && _password != "your-app-password";
    }

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string body)
    {
        if (!_isConfigured)
            return;

        using var message = new MailMessage();
        message.From = new MailAddress(_userName, _fromName);
        message.To.Add(new MailAddress(toEmail, toName));
        message.Subject = subject;
        message.Body = WrapInTemplate(body);
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_userName, _password),
            EnableSsl = _useSsl
        };

        try
        {
            await client.SendMailAsync(message);
            Debug.WriteLine($"[Email] Отправлено: {toEmail} | Тема: {subject}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Email] ОШИБКА отправки на {toEmail}: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[Email] InnerException: {ex.InnerException?.Message}");
        }
    }

    public async Task SendWelcomeEmailAsync(User user)
    {
        var body = $"""
            <h2>Добро пожаловать в {_agencyName}!</h2>
            <p>Здравствуйте, <strong>{user.FullName}</strong>!</p>
            <p>Вы успешно зарегистрировались. Теперь вам доступны:</p>
            <ul>
                <li>🏠 Просмотр каталога недвижимости</li>
                <li>⭐ Добавление объектов в избранное</li>
                <li>📅 Запись на просмотр объектов</li>
                <li>💬 Публикация отзывов</li>
            </ul>
            <p>Надеемся, что поможем вам найти идеальную недвижимость!</p>
            """;

        await SendEmailAsync(user.Email, user.FullName,
            $"Добро пожаловать в {_agencyName}!", body);
    }

    public async Task SendNewListingsNotificationAsync(IEnumerable<User> clients, int newCount)
    {
        var body = $"""
            <h2>У нас пополнился ассортимент! 🏠</h2>
            <p>В нашем каталоге появились новые объекты недвижимости.</p>
            <p>Сейчас доступно уже <strong>{newCount}</strong> активных предложений.</p>
            <p>Заходите и выбирайте — возможно, там уже есть то, что вы искали!</p>
            {(_agencySite.StartsWith("http") ? $"""<p><a href="{_agencySite}" style="color:#D4A5A5">Перейти в каталог →</a></p>""" : "")}
            """;

        var tasks = clients.Select(c =>
            SendEmailAsync(c.Email, c.FullName, $"Новые объекты в {_agencyName}!", body));
        await Task.WhenAll(tasks);
    }

    public async Task SendBulkEmailAsync(IEnumerable<User> recipients, string subject, string body)
    {
        var tasks = recipients.Select(u => SendEmailAsync(u.Email, u.FullName, subject, body));
        await Task.WhenAll(tasks);
    }

    public async Task SendAppointmentStatusChangedAsync(User client, string propertyTitle, string realtorName, string newStatus, DateTime slotStart)
    {
        var (statusText, icon) = newStatus switch
        {
            "confirmed" => ("подтверждена", "✅"),
            "cancelled" => ("отменена",     "❌"),
            _           => ("изменена",     "ℹ")
        };

        var body = $"""
            <h2>{icon} Запись на просмотр {statusText}</h2>
            <p>Здравствуйте, <strong>{client.FullName}</strong>!</p>
            <p>Ваша запись на просмотр объекта <strong>«{propertyTitle}»</strong> была {statusText}.</p>
            <table style="border-collapse:collapse;margin:12px 0">
                <tr><td style="padding:4px 12px 4px 0;color:#888">Объект:</td><td><strong>{propertyTitle}</strong></td></tr>
                <tr><td style="padding:4px 12px 4px 0;color:#888">Риелтор:</td><td>{realtorName}</td></tr>
                <tr><td style="padding:4px 12px 4px 0;color:#888">Дата:</td><td>{slotStart:dd.MM.yyyy HH:mm}</td></tr>
                <tr><td style="padding:4px 12px 4px 0;color:#888">Статус:</td><td><strong>{statusText.ToUpper()}</strong></td></tr>
            </table>
            {(newStatus == "confirmed" ? "<p>Пожалуйста, приходите вовремя. Риелтор ждёт вас!</p>" : "<p>Если у вас есть вопросы, свяжитесь с риелтором напрямую.</p>")}
            """;

        await SendEmailAsync(client.Email, client.FullName,
            $"{icon} Запись на просмотр {statusText} — {_agencyName}", body);
    }

    private string WrapInTemplate(string content) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;background:#f5f5f5;padding:20px">
          <div style="max-width:560px;margin:0 auto;background:white;border-radius:10px;overflow:hidden">
            <div style="background:#D4A5A5;padding:20px 30px">
              <h1 style="color:white;margin:0;font-size:20px">{_agencyName}</h1>
            </div>
            <div style="padding:30px;color:#333;line-height:1.6">
              {content}
            </div>
            <div style="background:#f9f9f9;padding:16px 30px;font-size:12px;color:#999;border-top:1px solid #eee">
              © {_agencyName}. Это автоматическое письмо, не отвечайте на него.
            </div>
          </div>
        </body>
        </html>
        """;
}
