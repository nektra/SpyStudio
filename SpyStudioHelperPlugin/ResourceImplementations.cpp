#include "stdafx.h"
#include "SerializerInheritors.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include <cassert>
#include <limits>
#include <memory>
#include <string>
#include <algorithm>

struct LangPair
{
  unsigned short id;
  const char *string;
  bool operator<(const LangPair &b) const
  {
    return this->id < b.id;
  }
};

static const LangPair lang_strings[] =
{
  { 0x0001, "ar" },
  { 0x0002, "bg" },
  { 0x0003, "ca" },
  { 0x0004, "zh-Hans" },
  { 0x0005, "cs" },
  { 0x0006, "da" },
  { 0x0007, "de" },
  { 0x0008, "el" },
  { 0x0009, "en" },
  { 0x000A, "es" },
  { 0x000B, "fi" },
  { 0x000C, "fr" },
  { 0x000D, "he" },
  { 0x000E, "hu" },
  { 0x000F, "is" },
  { 0x0010, "it" },
  { 0x0011, "ja" },
  { 0x0012, "ko" },
  { 0x0013, "nl" },
  { 0x0014, "no" },
  { 0x0015, "pl" },
  { 0x0016, "pt" },
  { 0x0018, "ro" },
  { 0x0019, "ru" },
  { 0x001A, "hr" },
  { 0x001B, "sk" },
  { 0x001C, "sq" },
  { 0x001D, "sv" },
  { 0x001E, "th" },
  { 0x001F, "tr" },
  { 0x0020, "ur" },
  { 0x0021, "id" },
  { 0x0022, "uk" },
  { 0x0023, "be" },
  { 0x0024, "sl" },
  { 0x0025, "et" },
  { 0x0026, "lv" },
  { 0x0027, "lt" },
  { 0x0029, "fa" },
  { 0x002A, "vi" },
  { 0x002B, "hy" },
  { 0x002C, "az" },
  { 0x002D, "eu" },
  { 0x002F, "mk" },
  { 0x0036, "af" },
  { 0x0037, "ka" },
  { 0x0038, "fo" },
  { 0x0039, "hi" },
  { 0x003E, "ms" },
  { 0x003F, "kk" },
  { 0x0040, "ky" },
  { 0x0041, "sw" },
  { 0x0043, "uz" },
  { 0x0044, "tt" },
  { 0x0046, "pa" },
  { 0x0047, "gu" },
  { 0x0049, "ta" },
  { 0x004A, "te" },
  { 0x004B, "kn" },
  { 0x004E, "mr" },
  { 0x004F, "sa" },
  { 0x0050, "mn" },
  { 0x0056, "gl" },
  { 0x0057, "kok" },
  { 0x005A, "syr" },
  { 0x0065, "dv" },
  { 0x007F, "" },
  { 0x0401, "ar-SA" },
  { 0x0402, "bg-BG" },
  { 0x0403, "ca-ES" },
  { 0x0404, "zh-TW" },
  { 0x0405, "cs-CZ" },
  { 0x0406, "da-DK" },
  { 0x0407, "de-DE" },
  { 0x0408, "el-GR" },
  { 0x0409, "en-US" },
  { 0x040A, "es-ES_tradnl" },
  { 0x040B, "fi-FI" },
  { 0x040C, "fr-FR" },
  { 0x040D, "he-IL" },
  { 0x040E, "hu-HU" },
  { 0x040F, "is-IS" },
  { 0x0410, "it-IT" },
  { 0x0411, "ja-JP" },
  { 0x0412, "ko-KR" },
  { 0x0413, "nl-NL" },
  { 0x0414, "nb-NO" },
  { 0x0415, "pl-PL" },
  { 0x0416, "pt-BR" },
  { 0x0418, "ro-RO" },
  { 0x0419, "ru-RU" },
  { 0x041A, "hr-HR" },
  { 0x041B, "sk-SK" },
  { 0x041C, "sq-AL" },
  { 0x041D, "sv-SE" },
  { 0x041E, "th-TH" },
  { 0x041F, "tr-TR" },
  { 0x0420, "ur-PK" },
  { 0x0421, "id-ID" },
  { 0x0422, "uk-UA" },
  { 0x0423, "be-BY" },
  { 0x0424, "sl-SI" },
  { 0x0425, "et-EE" },
  { 0x0426, "lv-LV" },
  { 0x0427, "lt-LT" },
  { 0x0429, "fa-IR" },
  { 0x042A, "vi-VN" },
  { 0x042B, "hy-AM" },
  { 0x042C, "az-Latn-AZ" },
  { 0x042D, "eu-ES" },
  { 0x042F, "mk-MK" },
  { 0x0436, "af-ZA" },
  { 0x0437, "ka-GE" },
  { 0x0438, "fo-FO" },
  { 0x0439, "hi-IN" },
  { 0x043E, "ms-MY" },
  { 0x043F, "kk-KZ" },
  { 0x0440, "ky-KG" },
  { 0x0441, "sw-KE" },
  { 0x0443, "uz-Latn-UZ" },
  { 0x0444, "tt-RU" },
  { 0x0446, "pa-IN" },
  { 0x0447, "gu-IN" },
  { 0x0449, "ta-IN" },
  { 0x044A, "te-IN" },
  { 0x044B, "kn-IN" },
  { 0x044E, "mr-IN" },
  { 0x044F, "sa-IN" },
  { 0x0450, "mn-MN" },
  { 0x0456, "gl-ES" },
  { 0x0457, "kok-IN" },
  { 0x045A, "syr-SY" },
  { 0x0465, "dv-MV" },
  { 0x0801, "ar-IQ" },
  { 0x0804, "zh-CN" },
  { 0x0807, "de-CH" },
  { 0x0809, "en-GB" },
  { 0x080A, "es-MX" },
  { 0x080C, "fr-BE" },
  { 0x0810, "it-CH" },
  { 0x0813, "nl-BE" },
  { 0x0814, "nn-NO" },
  { 0x0816, "pt-PT" },
  { 0x081A, "sr-Latn-CS" },
  { 0x081D, "sv-FI" },
  { 0x082C, "az-Cyrl-AZ" },
  { 0x083E, "ms-BN" },
  { 0x0843, "uz-Cyrl-UZ" },
  { 0x0C01, "ar-EG" },
  { 0x0C04, "zh-HK" },
  { 0x0C07, "de-AT" },
  { 0x0C09, "en-AU" },
  { 0x0C0A, "es-ES" },
  { 0x0C0C, "fr-CA" },
  { 0x0C1A, "sr-Cyrl-CS" },
  { 0x1001, "ar-LY" },
  { 0x1004, "zh-SG" },
  { 0x1007, "de-LU" },
  { 0x1009, "en-CA" },
  { 0x100A, "es-GT" },
  { 0x100C, "fr-CH" },
  { 0x1401, "ar-DZ" },
  { 0x1404, "zh-MO" },
  { 0x1407, "de-LI" },
  { 0x1409, "en-NZ" },
  { 0x140A, "es-CR" },
  { 0x140C, "fr-LU" },
  { 0x1801, "ar-MA" },
  { 0x1809, "en-IE" },
  { 0x180A, "es-PA" },
  { 0x180C, "fr-MC" },
  { 0x1C01, "ar-TN" },
  { 0x1C09, "en-ZA" },
  { 0x1C0A, "es-DO" },
  { 0x2001, "ar-OM" },
  { 0x2009, "en-JM" },
  { 0x200A, "es-VE" },
  { 0x2401, "ar-YE" },
  { 0x2409, "en-029" },
  { 0x240A, "es-CO" },
  { 0x2801, "ar-SY" },
  { 0x2809, "en-BZ" },
  { 0x280A, "es-PE" },
  { 0x2C01, "ar-JO" },
  { 0x2C09, "en-TT" },
  { 0x2C0A, "es-AR" },
  { 0x3001, "ar-LB" },
  { 0x3009, "en-ZW" },
  { 0x300A, "es-EC" },
  { 0x3401, "ar-KW" },
  { 0x3409, "en-PH" },
  { 0x340A, "es-CL" },
  { 0x3801, "ar-AE" },
  { 0x380A, "es-UY" },
  { 0x3C01, "ar-BH" },
  { 0x3C0A, "es-PY" },
  { 0x4001, "ar-QA" },
  { 0x400A, "es-BO" },
  { 0x440A, "es-SV" },
  { 0x480A, "es-HN" },
  { 0x4C0A, "es-NI" },
  { 0x500A, "es-PR" },
  { 0x7C04, "zh-Hant" }
};

static const size_t lang_strings_size = 203;

const char *get_language_string(unsigned short id)
{
  LangPair temp;
  temp.id = id;
  const LangPair *pair = std::lower_bound(lang_strings, lang_strings + lang_strings_size, temp);
  return pair->id == id ? pair->string : 0;
}

void FindResourceCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param;

  if (is_precall)
    return;

  param = params->GetAt(1);
  long type = param->GetIntResourceString();
  if (type > 0)
    abuffer->AddHexInteger(type);
  else
    abuffer->AddString(param->ReadString());

  param = params->GetAt(2);
  long name = param->GetIntResourceString();
  if (name > 0)
    abuffer->AddHexIntegerWithoutBasePrefix(name);
  else
    abuffer->AddString(param->ReadString());

  param = params->GetAt(3);
  long lang = param->GetIntResourceString();
  if (lang != 0)
  {
    const char *language_string = get_language_string((unsigned short)lang);
    if (!language_string)
      abuffer->AddEmptyString();
    else
      abuffer->AddString(language_string);
  }
  else
    abuffer->AddEmptyString();

  param = params->GetAt(0);
  this->AddOptionalModule(hcip, param->GetPointerVal());
}

void LoadResourceCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param;

  if (is_precall)
    return;

  param = params->GetAt(0);

  mword_t hmodule = param->GetSizeTVal();
  this->AddOptionalModule(hcip, param->GetSizeTVal());
}
