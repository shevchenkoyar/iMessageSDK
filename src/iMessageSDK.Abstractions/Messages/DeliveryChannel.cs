namespace iMessageSDK.Messages;

/// <summary>
/// The network a <see cref="Message"/> was, or will be, delivered over.
/// </summary>
public enum DeliveryChannel
{
    /// <summary>Delivered over Apple's iMessage service.</summary>
    IMessage,

    /// <summary>Delivered as SMS/MMS, or forwarded RCS relayed through a paired phone.</summary>
    SmsForwarding,
}
