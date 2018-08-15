using System.ComponentModel.DataAnnotations;

namespace MasterService.Models
{
    public enum RoomEquipment
    {
        None = 0x00,
        Display = 0x01,
        Telephone = 0x02,
        Whiteboard = 0x04,
        [Display(Name = "USB Speakerphone")]
        UsbSpeakerphone = 0x08,
        [Display(Name = "Mac Mini")]
        MacMini = 0x10,
        [Display(Name = "Skype Room System")]
        SkypeRoomSystem = 0x20,
        [Display(Name="Audio System")]
        AudioSystem = 0x40,
    }
}
