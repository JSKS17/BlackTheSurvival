using System;
using System.Collections.Generic;

namespace Lumia
{
    /// <summary>Presentation-only identity. Never changes a card or consumes gameplay randomness.</summary>
    public sealed class CombatVisualProfile
    {
        public readonly string motif, motion, primary, accent;
        public readonly float duration, impactAt;
        public readonly int projectiles, seed;
        public readonly bool ultimate;

        internal CombatVisualProfile(string motif, string motion, string primary, string accent,
            float duration, float impactAt, int projectiles, int seed, bool ultimate)
        {
            this.motif = motif; this.motion = motion; this.primary = primary; this.accent = accent;
            this.duration = duration; this.impactAt = impactAt;
            this.projectiles = projectiles; this.seed = seed; this.ultimate = ultimate;
        }
    }

    /// <summary>Original skill silhouettes, adapted to compact pixel effects in a fixed combat arena.</summary>
    public static class CombatVisualProfiles
    {
        sealed class Identity
        {
            public string primary, accent;
            public string[] skills;
        }

        static readonly Dictionary<CardDef, CombatVisualProfile> Cache = new Dictionary<CardDef, CombatVisualProfile>();
        static readonly CombatVisualProfile Empty = new CombatVisualProfile("orb", "cast", "91DFBD", "E8FAEC", .7f, .42f, 1, 0, false);

        public static CombatVisualProfile For(CardDef card)
        {
            if (card == null) return Empty;
            CombatVisualProfile result;
            if (!Cache.TryGetValue(card, out result))
            {
                result = Create(card);
                Cache.Add(card, result);
            }
            return result;
        }

        static CombatVisualProfile Create(CardDef card)
        {
            string id = card.id ?? "", key = (card.key ?? "").ToUpperInvariant();
            int separator = id.LastIndexOf('_');
            string subject = separator > 0 ? id.Substring(0, separator) : "";
            string primary = "91DFBD", accent = "E8FAEC", token = null;
            Identity identity;
            int index = "QWER".IndexOf(key, StringComparison.Ordinal);
            if (Subjects.TryGetValue(subject, out identity) && key.Length == 1 && index >= 0)
            {
                primary = identity.primary; accent = identity.accent; token = identity.skills[index];
            }
            Override(id, ref token, ref primary, ref accent);
            string[] palette;
            if (SkillPalettes.TryGetValue(id, out palette)) { primary = palette[0]; accent = palette[1]; }
            if (token == null)
                token = card.heal > 0 && card.damage <= 0 ? "heal" : card.block > 0 && card.damage <= 0 ? "shield"
                    : card.hits > 2 ? "barrage" : card.damage > 0 ? "slash" : "sigil";
            string[] parts = token.Split(':');
            string motif = parts[0];
            string motion = card.movement ? (parts.Length > 1 ? parts[1] : "dash")
                : parts.Length > 1 && parts[1] == "none" ? "none" : Melee(motif) && card.damage > 0 ? "strike" : "cast";
            bool ultimate = key == "R";
            int count = Math.Max(1, Math.Min(6, card.hits));
            if (motif == "barrage") count = Math.Max(3, count);
            if (motif == "blade_orbit") count = Math.Max(3, count);
            if (motif == "arcade") count = id == "nia_r" ? 6 : 1;
            if (id == "hyejin_r") count = 5;
            if (id == "lucia_w") count = 4;
            if (id == "tsubame_q") count = 3;
            if (subject == "adela") count = key == "R" ? 4 : key == "Q" ? 1 : 2;
            float duration = ultimate ? 1.04f : card.movement ? .86f : count > 2 ? .94f : .76f;
            float impact = motif == "sniper" ? .5f : motif == "bomb" || motif == "trap" ? .46f : .38f;
            return new CombatVisualProfile(motif, motion, primary, accent, duration, impact, count, StableSeed(id), ultimate);
        }

        static bool Melee(string motif)
        {
            return motif == "slash" || motif == "thrust" || motif == "chain" || motif == "earth" || motif == "blood";
        }

        static int StableSeed(string id)
        {
            unchecked { uint value = 2166136261; foreach (char c in id) value = (value ^ c) * 16777619; return (int)(value & 0x7fffffff); }
        }

        static void Override(string id, ref string token, ref string primary, ref string accent)
        {
            switch (id)
            {
                case "basic_attack": token = "slash"; primary = "F0D3A0"; accent = "FFF6D9"; break;
                case "basic_guard": token = "shield:none"; primary = "83CDBA"; accent = "D9F9E8"; break;
                case "weapon_glove": token = "earth"; primary = "F1B77C"; accent = "FFF0C8"; break;
                case "weapon_tonfa": token = "shield"; primary = "90C9D9"; accent = "EFFAFF"; break;
                case "weapon_bat": token = "earth"; primary = "C79A76"; accent = "FFE6B0"; break;
                case "weapon_whip": token = "chain"; primary = "C6A2DA"; accent = "F4DDFF"; break;
                case "weapon_throw": token = "orb"; primary = "9DD2EA"; accent = "EDF9FF"; break;
                case "weapon_shuriken": token = "trap"; primary = "ADB9CD"; accent = "EDF1FF"; break;
                case "weapon_bow": token = "arrow"; primary = "ECCB83"; accent = "FFF0CA"; break;
                case "weapon_crossbow": token = "arrow"; primary = "DBAB7B"; accent = "FFE3AC"; break;
                case "weapon_pistol": token = "bullet:retreat"; primary = "F3C77B"; accent = "FFF1CA"; break;
                case "weapon_rifle": token = "barrage"; primary = "F4AC64"; accent = "FFE9A0"; break;
                case "weapon_sniper": token = "sniper"; primary = "D9E8F5"; accent = "F9FDFF"; break;
                case "weapon_hammer": token = "earth"; primary = "CFB48C"; accent = "FCE6AC"; break;
                case "weapon_axe": token = "blood:orbit"; primary = "E66B76"; accent = "FFC6B7"; break;
                case "weapon_dagger": token = "slash:blink"; primary = "A2A5DB"; accent = "D4D9FF"; break;
                case "weapon_sword": token = "slash:dash"; primary = "BBCDE1"; accent = "F0F9FF"; break;
                case "weapon_dual": token = "blade_orbit:dash"; primary = "D8B6E9"; accent = "FFE5FC"; break;
                case "weapon_spear": token = "thrust"; primary = "D6BD8A"; accent = "FFF3C7"; break;
                case "weapon_nunchaku": token = "blade_orbit"; primary = "E3B579"; accent = "FFEEAE"; break;
                case "weapon_rapier": token = "thrust:dash"; primary = "C1D5EF"; accent = "F3FAFF"; break;
                case "weapon_guitar": token = "music"; primary = "EBA9CC"; accent = "FFE6F6"; break;
                case "weapon_camera": token = "camera"; primary = "E9E0BD"; accent = "FFFFF1"; break;
                case "weapon_arcana": token = "paper"; primary = "C9ACE8"; accent = "F4DEFF"; break;
                case "weapon_vf": token = "orb"; primary = "73D7BC"; accent = "C9FFE2"; break;
                case "tactical_blink": token = "prism:blink"; primary = "8EE4ED"; accent = "F0FFFF"; break;
                case "tactical_wind": token = "heal"; primary = "8CDDAD"; accent = "E5FFCC"; break;
                case "tactical_plasma": token = "lightning:dash"; primary = "80CFF5"; accent = "E6FFFF"; break;
            }
        }

        static Dictionary<string, Identity> BuildSubjects()
        {
            var result = new Dictionary<string, Identity>(StringComparer.Ordinal);
            foreach (string row in SubjectRows)
            {
                var parts = row.Split('|');
                result.Add(parts[0], new Identity { primary = parts[1], accent = parts[2], skills = parts[3].Split(' ') });
            }
            return result;
        }

        // Q W E R. Motion suffixes are only used when the canonical movement flag is true.
        static readonly string[] SubjectRows =
        {
            "nia|C92FCC|EE6FED|arcade orb arcade arcade",
            "jackie|FB4C14|FEFDA5|slash blood slash:leap blade_orbit",
            "aya|CC9157|C3B5AE|bullet barrage bullet:retreat bomb",
            "hyunwoo|CC3D0E|F7F6DA|earth shield earth:dash earth",
            "yuki|6B80CC|FDFBFC|slash shield slash:dash slash",
            "hyejin|8C58CC|E1C0E5|paper sigil paper:blink blade_orbit",
            "sua|A0CC1A|EFF5DD|paper shield thrust:dash paper",
            "isol|F5F76E|F9EDDC|bomb barrage trap trap",
            "nadine|CC4B38|E7ADA5|arrow trap chain star",
            "emma|38B5CC|C7D0E2|paper star puppet star:blink",
            "charlotte|3CA4CC|6CC9E2|star heal star:dash sigil",
            "nathapon|1184CC|6CD9FC|camera camera camera camera",
            "nicky|CC3F03|FDF994|earth shield:dash thrust earth:leap",
            "daniel|CC4DBA|DC9DDD|slash sigil slash:blink slash:blink",
            "tia|F7E662|E8B542|paper prism paper:dash prism",
            "laura|E516FC|FFFFFE|chain paper chain:dash slash",
            "lenox|61CCB0|EFFCAB|chain chain chain chain",
            "leon|2D99D4|49D4F5|water water water:leap water:dash",
            "rozzi|F7DB80|FDFAEF|bullet:dash barrage bullet:leap bomb",
            "luke|CC9B43|A8E9DD|thrust:dash shield thrust:leap water:dash",
            "dailin|FDBC54|FFFFBF|thrust:dash heal thrust earth",
            "rio|A0C8DC|DBDBC1|arrow arrow arrow:leap arrow",
            "martina|37DDFB|A9EEFA|camera camera camera:retreat camera",
            "mai|ECBC43|A6FD97|chain shield chain:dash shield",
            "markus|D9520D|E7DBC5|slash slash earth:dash earth:leap",
            "magnus|FCEF22|FDF98B|bullet blade_orbit earth flame:dash",
            "vanya|116FCC|40AFD8|star shield wind sigil",
            "barbara|1EC3ED|A0C2D3|trap lightning lightning prism",
            "bernice|E19122|F2D990|bullet trap arrow chain",
            "bianca|CC1B27|E7989D|blood shield blood:leap blood",
            "celine|1D99CC|DAF9FC|bomb bomb wind bomb",
            "sho|FE573A|EFCB7E|flame heal flame flame",
            "shoichi|CC1F55|E799B2|slash slash:blink slash blade_orbit",
            "sissela|358ECC|C1CBD0|orb shield chain sigil",
            "silvia|1068CC|D6F4FC|bullet flame:dash orb flame",
            "adela|CEA23F|E5C664|chess chess chess chess",
            "adriana|DE8241|DEB36B|flame trap flame:retreat flame",
            "adina|4B7CD8|61B3DC|star star star sigil",
            "isaac|CC3663|E89EB3|thrust:dash shield chain earth",
            "alex|037FCC|E7FAFD|bullet sigil bomb bomb",
            "jan|B1E99D|DDFACF|thrust slash thrust:dash sigil",
            "estelle|EB4F36|F6E197|thrust heal shield:dash wind",
            "aiden|2F6ACC|C2E7F7|lightning lightning lightning:retreat lightning:blink",
            "echion|ED3A7A|FFFFFF|slash shield slash:dash prism",
            "elena|0AF8FD|F2FFFF|ice ice:orbit ice ice",
            "johann|EABE49|FDE36D|star sigil paper heal",
            "william|5295CC|C0E2F1|orb sigil orb orb:blink",
            "irem|CC404C|ECA98C|orb sigil star star:orbit",
            "eva|CC268D|FFF0F9|orb orb prism prism",
            "ian|982FCC|E39BEB|slash blade_orbit slash sigil",
            "eleven|D18A59|EEDAB0|earth sigil earth blade_orbit",
            "zahir|A29EE8|F7F8FE|orb blade_orbit wind sigil",
            "jenny|D671E6|FAEBFE|star paper star:retreat sigil",
            "camilo|D44259|E7C5C5|slash slash:orbit thrust blade_orbit",
            "karla|CC0D08|FDF1B6|chain chain chain chain",
            "cathy|FC2487|C3B7C7|slash:dash slash chain sigil:dash",
            "chloe|D0A34F|FFFEEF|puppet puppet:leap chain puppet",
            "chiara|FE97FF|D85AF4|poison shield chain blood",
            "tazia|5CACEF|9AFBFF|prism prism prism prism",
            "theodore|33C1ED|F1FEFF|sniper prism bomb lightning",
            "felix|A1E938|DBFF4F|thrust:dash thrust:dash wind thrust",
            "priya|96CC70|FFFFFF|flower music flower music",
            "fiora|6CA6D5|E4F9FD|thrust slash thrust:retreat thrust",
            "piolo|2174D0|ACEDF2|blade_orbit blade_orbit chain earth:leap",
            "hart|CC43AB|F8BAE9|music music music music",
            "haze|FBEA6E|CEA14C|bomb barrage bullet:retreat bomb",
            "debi_marlene|70E7F9|6076DD|slash blade_orbit slash:dash barrage",
            "arda|56C9F3|2E9ACB|paper prism earth sigil",
            "abigail|41AAE9|ACF1F8|slash slash slash:blink slash",
            "alonso|439CE7|86B4EB|lightning shield earth:leap lightning",
            "leni|D8741E|F8B951|bomb earth:leap music trap",
            "tsubame|7B44CC|EA9AE9|star wind trap slash",
            "kenneth|F8C93D|FEEB90|slash flame slash:dash flame",
            "katja|3BC3CC|69F8C9|sniper trap chain sniper",
            "darko|41F4F3|9DF9F9|thrust chain earth:dash sigil",
            "lenore|27A1DC|56D9EE|music music music music",
            "garnet|73C3CC|D5DADA|earth shield chain:dash blood",
            "yumin|536CE1|FFFEFF|wind wind wind:dash wind",
            "hisui|CC322F|FBFFFF|slash thrust slash:dash slash",
            "justyna|1E63CC|FDF9FE|barrage lightning lightning:dash prism:leap",
            "istvan|18C8E5|A8D6E0|thrust:dash slash:dash thrust:dash prism:leap",
            "xuelin|0EC3CC|2EE2E4|blade_orbit shield slash:blink blade_orbit",
            "henry|F8F08C|FFFFDE|time time time:blink time",
            "blair|24C4CC|FDFDFD|slash thrust blade_orbit blade_orbit",
            "mirka|CC2970|EC7CCA|earth shield:dash earth:retreat earth:leap",
            "fenrir|FDE946|DE7618|slash chain sigil slash:leap",
            "coraline|3392CC|FEFDFE|prism prism chain prism",
            "bihyung|58FDAE|F9FFFD|sigil paper star sigil:leap",
            "craver|CC3131|FEE3E3|bullet slash bullet:retreat barrage:blink",
            "lucia|AE2DCC|E0B8E7|bullet barrage prism:dash sniper",
            "ceres|F560E8|FFFFFF|thrust:dash slash shield:leap shield"
        };
        static readonly Dictionary<string, Identity> Subjects = BuildSubjects();

        static Dictionary<string, string[]> BuildSkillPalettes()
        {
            var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (string row in SkillPaletteRows)
            {
                var parts = row.Split('|');
                result.Add(parts[0], new[] { parts[1], parts[2] });
            }
            return result;
        }

        // Effect colors sampled from each original default-skill icon in the cached reference set.
        // The explicit shape/motion table above supplies identity; this table supplies its own colors.
        static readonly string[] SkillPaletteRows =
        {
            "nia_q|C92FCC|EE6FED",
            "nia_w|D642DA|EDB0F0",
            "nia_e|843FCC|D164E6",
            "nia_r|A838CC|F6BFFC",
            "jackie_q|FB4C14|FEFDA5",
            "jackie_w|CC2D2E|E7A0A0",
            "jackie_e|F79B28|FAF5AA",
            "jackie_r|D75C08|E1CF9D",
            "aya_q|CC9157|C3B5AE",
            "aya_w|BA52CC|FBECEF",
            "aya_e|4BD3E5|D9E3E8",
            "aya_r|F7AC3A|FFFFFF",
            "hyunwoo_q|CC3D0E|F7F6DA",
            "hyunwoo_w|71D5F7|BEEBFB",
            "hyunwoo_e|3190CC|AFBAC2",
            "hyunwoo_r|E69D69|FFFBA5",
            "yuki_q|6B80CC|FDFBFC",
            "yuki_w|CC8C82|FFE1D1",
            "yuki_e|8B9ADD|ECE2FB",
            "yuki_r|CC44C9|F7B4B4",
            "hyejin_q|8C58CC|E1C0E5",
            "hyejin_w|91CFF9|FFFEFF",
            "hyejin_e|6893CC|EFDABE",
            "hyejin_r|7054CC|F4F8FF",
            "sua_q|A0CC1A|EFF5DD",
            "sua_w|BADA6F|B5FAE1",
            "sua_e|9ECC65|F2FCE1",
            "sua_r|BFCC09|DCEAE1",
            "isol_q|F5F76E|F9EDDC",
            "isol_w|F9BC62|FFF3DE",
            "isol_e|C0CC62|9EC5B7",
            "isol_r|F3BC48|F7DF76",
            "nadine_q|CC4B38|E7ADA5",
            "nadine_w|E1BA76|F0F1D5",
            "nadine_e|4DCCC3|CDF9F6",
            "nadine_r|4DCCC6|EBF8F3",
            "emma_q|38B5CC|C7D0E2",
            "emma_w|52CCC9|D0E5E5",
            "emma_e|20B6CC|5DF1F2",
            "emma_r|80D9DF|E2FEFD",
            "charlotte_q|3CA4CC|6CC9E2",
            "charlotte_w|2399CC|A6F2EC",
            "charlotte_e|04BBCC|F4F8FD",
            "charlotte_r|2584CC|66E0DE",
            "nathapon_q|1184CC|6CD9FC",
            "nathapon_w|0081E0|ADDAF2",
            "nathapon_e|137FCD|F5FBFC",
            "nathapon_r|2598CC|E5D0C9",
            "nicky_q|CC3F03|FDF994",
            "nicky_w|FAC643|F0841D",
            "nicky_e|FB962F|D17611",
            "nicky_r|E67109|FEF98F",
            "daniel_q|CC4DBA|DC9DDD",
            "daniel_w|7649CC|D58BE0",
            "daniel_e|A146CC|C661E0",
            "daniel_r|F675E3|D954C8",
            "tia_q|F7E662|E8B542",
            "tia_w|CD9C32|F6E6B3",
            "tia_e|EDCE16|D09712",
            "tia_r|ABF766|EDFC9B",
            "laura_q|E516FC|FFFFFE",
            "laura_w|A33FCC|DCC8EE",
            "laura_e|7B40CC|BCA3DB",
            "laura_r|9241CC|E499EC",
            "lenox_q|61CCB0|EFFCAB",
            "lenox_w|A0C8DC|D1EDAF",
            "lenox_e|66CCB8|F8FEC4",
            "lenox_r|5ECCBD|FDFEF6",
            "leon_q|2D99D4|49D4F5",
            "leon_w|2B8CCC|C7F2FB",
            "leon_e|38B3E5|6EC5D9",
            "leon_r|2190CC|D9E6EB",
            "rozzi_q|F7DB80|FDFAEF",
            "rozzi_w|EDC965|D19F4C",
            "rozzi_e|CC6519|FEF39C",
            "rozzi_r|F9DA6B|FFFDC9",
            "luke_q|CC9B43|A8E9DD",
            "luke_w|80AECC|E3E6EB",
            "luke_e|93A8CC|CED7E7",
            "luke_r|2D7ACC|BCE8FA",
            "dailin_q|FDBC54|FFFFBF",
            "dailin_w|CC8734|FBF383",
            "dailin_e|E1B529|C4E2E4",
            "dailin_r|F5E13A|DB8C21",
            "rio_q|A0C8DC|DBDBC1",
            "rio_w|A0C8DC|D6EFED",
            "rio_e|91C2CC|CDE3E7",
            "rio_r|CDBD7A|FCF9CC",
            "martina_q|37DDFB|A9EEFA",
            "martina_w|018BCC|B8C8EF",
            "martina_e|147ED3|A4D4F6",
            "martina_r|1F88CC|36BAE5",
            "mai_q|ECBC43|A6FD97",
            "mai_w|3BEDD5|D7FFF9",
            "mai_e|4ECCBA|D3F1D4",
            "mai_r|86C4CC|D9F0E9",
            "markus_q|D9520D|E7DBC5",
            "markus_w|F7B442|FBE26C",
            "markus_e|F7FA6D|E0B858",
            "markus_r|EFDC3A|FDFC9E",
            "magnus_q|FCEF22|FDF98B",
            "magnus_w|E46113|FEFCAF",
            "magnus_e|D7C17C|F9FCEF",
            "magnus_r|5994CC|DAE7E8",
            "vanya_q|116FCC|40AFD8",
            "vanya_w|49C5DB|6EE7EF",
            "vanya_e|1496CC|9EF5EA",
            "vanya_r|4B69CC|A5D7EC",
            "barbara_q|1EC3ED|A0C2D3",
            "barbara_w|2BBFF3|B9FEF3",
            "barbara_e|4C9ED5|C3F4FE",
            "barbara_r|71B8CC|FBF9EC",
            "bernice_q|E19122|F2D990",
            "bernice_w|CC7D13|EEEDEB",
            "bernice_e|CC7B29|E3CF44",
            "bernice_r|F8CA33|F0761D",
            "bianca_q|CC1B27|E7989D",
            "bianca_w|CC3A3A|E7A6A6",
            "bianca_e|CC1348|EB9BB1",
            "bianca_r|CB5BCC|E7B4E7",
            "celine_q|1D99CC|DAF9FC",
            "celine_w|05A0CC|81EEF3",
            "celine_e|5268CC|C1F1FD",
            "celine_r|147FCC|FAF8EF",
            "sho_q|FE573A|EFCB7E",
            "sho_w|CC6136|D8D0B7",
            "sho_e|CC3356|E2FAFD",
            "sho_r|E9BC36|F8EFA1",
            "shoichi_q|CC1F55|E799B2",
            "shoichi_w|D22144|FFFFFF",
            "shoichi_e|CC2149|CDBECD",
            "shoichi_r|CC2A4D|F7FAFD",
            "sissela_q|358ECC|C1CBD0",
            "sissela_w|3B8ECC|88B9D0",
            "sissela_e|2A92CC|ACEFF9",
            "sissela_r|30BAD5|F6FFFE",
            "silvia_q|1068CC|D6F4FC",
            "silvia_w|0653CC|C5E3FB",
            "silvia_e|1E54CC|85E6F9",
            "silvia_r|CC750E|FEFD7D",
            "adela_q|CEA23F|E5C664",
            "adela_w|E2B347|F2E08B",
            "adela_e|CC933E|DEB55D",
            "adela_r|F9ED56|D9AD3F",
            "adriana_q|DE8241|DEB36B",
            "adriana_w|DF8A22|F0CA9B",
            "adriana_e|E3462A|AFE0D5",
            "adriana_r|D8843D|D0B26F",
            "adina_q|4B7CD8|61B3DC",
            "adina_w|3DB6DF|88FEFF",
            "adina_e|4F45CC|F6F8FD",
            "adina_r|1768CC|B7FCFE",
            "isaac_q|CC3663|E89EB3",
            "isaac_w|DF1B43|E8C8CF",
            "isaac_e|DD0246|F8969B",
            "isaac_r|FE4184|FBDACE",
            "alex_q|037FCC|E7FAFD",
            "alex_w|1B9BD1|6FDCED",
            "alex_e|0665CC|15B2F0",
            "alex_r|306ECC|77CFEB",
            "jan_q|B1E99D|DDFACF",
            "jan_w|5ECCAD|C9ED8F",
            "jan_e|6ECC87|C6EFB0",
            "jan_r|B7E67A|ECFF9C",
            "estelle_q|EB4F36|F6E197",
            "estelle_w|F1C445|D48B35",
            "estelle_e|FAAA31|FCEB55",
            "estelle_r|CC471F|F5E488",
            "aiden_q|2F6ACC|C2E7F7",
            "aiden_w|3894E7|F4FFFF",
            "aiden_e|81A1F2|C1C9F7",
            "aiden_r|55B1E9|FEFFFF",
            "echion_q|ED3A7A|FFFFFF",
            "echion_w|CC075A|DF79B6",
            "echion_e|CC0145|FFFFFF",
            "echion_r|D04691|F6C9E5",
            "elena_q|0AF8FD|F2FFFF",
            "elena_w|0A67CC|75FFFF",
            "elena_e|035BCC|B4C1C7",
            "elena_r|0193CD|BAECFE",
            "johann_q|EABE49|FDE36D",
            "johann_w|2941CC|FFFFFD",
            "johann_e|CDB469|DFCF93",
            "johann_r|CCA61C|FEFCE0",
            "william_q|5295CC|C0E2F1",
            "william_w|3EA0CC|80B9CA",
            "william_e|96BFDA|FEFEFD",
            "william_r|508CCE|E1EFF3",
            "irem_q|CC404C|ECA98C",
            "irem_w|CC262D|FBCDD3",
            "irem_e|CC1B32|D6828A",
            "irem_r|E5A75F|E7E6EA",
            "eva_q|CC268D|FFF0F9",
            "eva_w|D03490|FEECFA",
            "eva_e|E42B65|E7B5CF",
            "eva_r|EC48A8|FBC4E8",
            "ian_q|982FCC|E39BEB",
            "ian_w|CF6CE1|ECD2F3",
            "ian_e|DA42E9|D28DC7",
            "ian_r|CC1995|E797CF",
            "eleven_q|D18A59|EEDAB0",
            "eleven_w|F58BC4|C5CCDD",
            "eleven_e|CC2965|F888C8",
            "eleven_r|CC966D|FEE3E4",
            "zahir_q|A29EE8|F7F8FE",
            "zahir_w|6C90CC|EEFFFC",
            "zahir_e|6A98E3|FFFED4",
            "zahir_r|F9DBA5|FFFFD7",
            "jenny_q|D671E6|FAEBFE",
            "jenny_w|D46F2D|F6D470",
            "jenny_e|DC2EEA|E9CAE7",
            "jenny_r|CDA56F|F8F09C",
            "camilo_q|D44259|E7C5C5",
            "camilo_w|CC2653|FFFEF2",
            "camilo_e|FF3D73|FFF5DD",
            "camilo_r|F22952|FED5C3",
            "karla_q|CC0D08|FDF1B6",
            "karla_w|FF7F03|F7DC8D",
            "karla_e|EA4D20|FEF1C1",
            "karla_r|E05428|FFFCCC",
            "cathy_q|FC2487|C3B7C7",
            "cathy_w|E8207D|FEFBFC",
            "cathy_e|D00079|E5E4EF",
            "cathy_r|CC2563|FEF6FD",
            "chloe_q|D0A34F|FFFEEF",
            "chloe_w|CE943B|FDFEFF",
            "chloe_e|FAD765|FFF7E3",
            "chloe_r|CC9854|E7DABF",
            "chiara_q|FE97FF|D85AF4",
            "chiara_w|D114C0|F6B9A0",
            "chiara_e|E358F0|FDE0EE",
            "chiara_r|CC24B0|D961DA",
            "tazia_q|5CACEF|9AFBFF",
            "tazia_w|6E5CCC|B7B7D9",
            "tazia_e|7186DD|CDD8F7",
            "tazia_r|5CCBF2|F1FBFE",
            "theodore_q|33C1ED|F1FEFF",
            "theodore_w|3587EE|8FFAFE",
            "theodore_e|29F8FE|DDF9FB",
            "theodore_r|20E9FC|8DFCFE",
            "felix_q|A1E938|DBFF4F",
            "felix_w|4ED54E|D6FBD2",
            "felix_e|59CC1C|BFDB82",
            "felix_r|3DCC14|8CF428",
            "priya_q|96CC70|FFFFFF",
            "priya_w|B8CC12|FEFF4E",
            "priya_e|B5CC29|FFFF89",
            "priya_r|FFFF24|DCFE82",
            "fiora_q|6CA6D5|E4F9FD",
            "fiora_w|D53673|FFFFFF",
            "fiora_e|4484CC|A2B7E4",
            "fiora_r|CC59A1|EA87DD",
            "piolo_q|2174D0|ACEDF2",
            "piolo_w|1763CC|F5FCFF",
            "piolo_e|5ED0F9|E7FBFE",
            "piolo_r|30A8E0|ACE2FC",
            "hart_q|CC43AB|F8BAE9",
            "hart_w|F7AB49|FFFDD3",
            "hart_e|CC90C6|FFABFE",
            "hart_r|CC2D5A|FEFEFE",
            "haze_q|FBEA6E|CEA14C",
            "haze_w|E99D33|EFC458",
            "haze_e|ED9E48|F9EBA1",
            "haze_r|CC4510|EFD179",
            "debi_marlene_q|70E7F9|6076DD",
            "debi_marlene_w|3B78D5|6194E9",
            "debi_marlene_e|CC3A3D|F9E1DD",
            "debi_marlene_r|DA7A8B|9989C5",
            "arda_q|56C9F3|2E9ACB",
            "arda_w|1B86CC|76B2DA",
            "arda_e|06CEEB|048EC9",
            "arda_r|2887CC|63C7E2",
            "abigail_q|41AAE9|ACF1F8",
            "abigail_w|1C3FCC|DDF2FB",
            "abigail_e|4689D3|C8EBF9",
            "abigail_r|482BCC|93B7EA",
            "alonso_q|439CE7|86B4EB",
            "alonso_w|3684CC|FEEEB2",
            "alonso_e|347ECC|76B7D0",
            "alonso_r|086BCC|93D4E8",
            "leni_q|D8741E|F8B951",
            "leni_w|26CDE3|62EBFA",
            "leni_e|CC2D5C|ED91D9",
            "leni_r|F2AD0E|FBF9F5",
            "tsubame_q|7B44CC|EA9AE9",
            "tsubame_w|9655CC|8EA4C1",
            "tsubame_e|CC86A5|E7C8D6",
            "tsubame_r|B836E4|FFFFFF",
            "kenneth_q|F8C93D|FEEB90",
            "kenneth_w|F7B223|F96713",
            "kenneth_e|E4351B|FEFD93",
            "kenneth_r|F2A537|F9EC73",
            "katja_q|3BC3CC|69F8C9",
            "katja_w|41C4CC|63F0F0",
            "katja_e|44B2CC|7FECF7",
            "katja_r|1BCCCB|89F2F3",
            "darko_q|41F4F3|9DF9F9",
            "darko_w|CDA36F|ECD09D",
            "darko_e|0ACC8E|9EECD6",
            "darko_r|38CCC6|5DF1EF",
            "lenore_q|27A1DC|56D9EE",
            "lenore_w|159BCF|54B9DB",
            "lenore_e|3EC7E6|699BC9",
            "lenore_r|0AAAD9|4CBBC0",
            "garnet_q|73C3CC|D5DADA",
            "garnet_w|18C7CC|C95895",
            "garnet_e|57BFCC|F8FAFA",
            "garnet_r|EA61A8|F3C7DE",
            "yumin_q|536CE1|FFFEFF",
            "yumin_w|6A88F8|BCACE6",
            "yumin_e|595ECC|FFFFFF",
            "yumin_r|694BE8|E3E1FE",
            "hisui_q|CC322F|FBFFFF",
            "hisui_w|E73B2C|FEFBE6",
            "hisui_e|DF8367|E0C5B5",
            "hisui_r|CC2C29|F8D9B6",
            "justyna_q|1E63CC|FDF9FE",
            "justyna_w|026FCD|E9A3EE",
            "justyna_e|2B8FEC|C3F2FA",
            "justyna_r|2481EE|57AFF9",
            "istvan_q|18C8E5|A8D6E0",
            "istvan_w|2BE9F0|E1FEFE",
            "istvan_e|40B0EE|66DFFD",
            "istvan_r|2377CC|FAFDFE",
            "xuelin_q|0EC3CC|2EE2E4",
            "xuelin_w|36C0CC|B9FAF8",
            "xuelin_e|15C1CC|70F0EF",
            "xuelin_r|22C6CC|F6F5D2",
            "henry_q|F8F08C|FFFFDE",
            "henry_w|CCA94D|FFFFFE",
            "henry_e|CC8E19|FEF5A3",
            "henry_r|D4A43B|DBE49C",
            "blair_q|24C4CC|FDFDFD",
            "blair_w|33CED1|55FAFA",
            "blair_e|30DBE6|80FEFE",
            "blair_r|6CCBCC|9FDEDC",
            "mirka_q|CC2970|EC7CCA",
            "mirka_w|CC3C86|F3ADD9",
            "mirka_e|CC44AB|FD82D4",
            "mirka_r|CC258A|F5BBE6",
            "fenrir_q|FDE946|DE7618",
            "fenrir_w|EFBF28|FEF76B",
            "fenrir_e|F39F13|F7D157",
            "fenrir_r|F7D540|FDFAAE",
            "coraline_q|3392CC|FEFDFE",
            "coraline_w|1C95EE|FFFEFC",
            "coraline_e|5587D3|FBF5FA",
            "coraline_r|E477EC|6694C1",
            "bihyung_q|58FDAE|F9FFFD",
            "bihyung_w|CC1726|FFCEF1",
            "bihyung_e|CC3253|FFFCFD",
            "bihyung_r|CE475C|FFFFFF",
            "craver_q|CC3131|FEE3E3",
            "craver_w|FD4E47|FBDFE1",
            "craver_e|FF6860|FCF5F4",
            "craver_r|E00108|F366B0",
            "lucia_q|AE2DCC|E0B8E7",
            "lucia_w|C00ACF|F6B6F9",
            "lucia_e|B331CC|F2D1F4",
            "lucia_r|E556DE|FFFFFF",
            "ceres_q|F560E8|FFFFFF",
            "ceres_w|F858F5|FEE5FD",
            "ceres_e|B728CC|FCE7FC",
            "ceres_r|30A0CC|B5CAA8",
        };
        static readonly Dictionary<string, string[]> SkillPalettes = BuildSkillPalettes();
    }
}
