namespace TicketingEngine.Domain.ValueObjects;

public enum SeatStatus       { Available, Reserved, Sold, Unavailable }
public enum OrderStatus      { Pending, Paid, Expired, Cancelled }
public enum ReservationStatus{ Pending, Confirmed, Expired, Cancelled }
public enum PaymentStatus    { Pending, Succeeded, Failed, Refunded }
public enum EventStatus      { Draft, Published, OnSale, SoldOut, Cancelled, Completed }
public enum UserRole         { Customer, Admin, Organizer }
