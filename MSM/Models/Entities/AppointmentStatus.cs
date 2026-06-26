namespace MSM.Models.Entities;

public enum AppointmentStatus
{
    // Клиент записался, риелтор еще не подтвердил
    New, 
    // Риелтор подтвердил встречу
    Confirmed,
    // Отменена (клиентом или риелтором)
    Cancelled,
    // Встреча состоялась 
    Completed
}
