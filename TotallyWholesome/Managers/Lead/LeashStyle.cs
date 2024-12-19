using TotallyWholesome.Managers.Lead.LeadComponents;
using UnityEngine;

namespace TotallyWholesome.Managers.Lead
{
    public enum LeashStyle
    {
        [LeashConfig]
        Classic,
        [LeashConfig(lineTextureMode: LineTextureMode.Stretch)]
        Gradient,
        [LeashConfig(lineTextureMode: LineTextureMode.Stretch)]
        Magic,
        [LeashConfig]
        Chain,
        [LeashConfig]
        Leather,
        [LeashConfig]
        Amogus,
        [LeashConfig]
        Custom,

        //Pride Flags
        [LeashConfig("Pride")]
        LGBT,
        [LeashConfig("Pride")]
        Bisexual,
        [LeashConfig("Pride")]
        Polysexual,
        [LeashConfig("Pride")]
        Pansexual,
        [LeashConfig("Pride")]
        Lesbian,
        [LeashConfig("Pride")]
        Gay,
        [LeashConfig("Pride")]
        Asexual,
        [LeashConfig("Pride")]
        Trans,
        [LeashConfig("Pride")]
        Nonbinary,
        [LeashConfig("Pride")]
        Genderfluid,

        //Seasonal
        [LeashConfig("Seasonal",50, LineTextureMode.Tile, 0f)]
        Christmas
    }
}