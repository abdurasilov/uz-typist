# UzTypist

UzTypist — Windows uchun fon rejimida ishlaydigan yordamchi dastur. U klaviaturadagi `'` va `"` tugmalarini kontekstga qarab oʻzbek lotin alifbosida ishlatiladigan toʻgʻri Unicode belgilarga almashtiradi. Dastur oʻzining oynasiga ega emas va tizim boʻylab ishlaydi, shuning uchun brauzer, matn muharriri, messenger yoki IDE — qaysi dasturda yozilayotganidan qatʼi nazar amal qiladi.

Oʻzbek lotin yozuvida **Oʻ** va **Gʻ** harflari, shuningdek tutuq belgisi uchun standart klaviaturada boʻlmagan maxsus belgilar talab qilinadi. Koʻpchilik ular oʻrniga oddiy apostrof (`'`) qoʻyadi, natijada matn Unicode jihatidan notoʻgʻri boʻladi. UzTypist tugma bosilgan paytda kursor oldidagi belgini oʻqib, oddiy apostrof yoki qoʻshtirnoq oʻrniga kerakli belgini qoʻyadi.

## Almashtirish qoidalari

| Kiritish | Kontekst | Natija | Unicode |
|----------|----------|--------|---------|
| `'` | oldida `O`, `o`, `G` yoki `g` harfi | ʻ | U+02BB — modifier letter turned comma |
| `'` | boshqa har qanday holatda | ʼ | U+02BC — modifier letter apostrophe |
| `"` | soʻz boshida (oldida boʻshliq, `(` yoki matn boshi) | “ | U+201C — left double quotation mark |
| `"` | soʻz ichida yoki oxirida | ” | U+201D — right double quotation mark |

Katta-kichik harf `Shift` va `Caps Lock` holatiga qarab aniqlanadi.

## Texnologiyalar

| Texnologiya | Vazifasi |
|-------------|----------|
| C# / .NET 10 (`net10.0-windows`) | Asosiy til va maqsadli platforma |
| Windows Forms | Tray ikonka va kontekst menyu (dasturda koʻrinadigan oyna yoʻq) |
| WPF (`System.Windows.Automation`) | Fokusdagi elementdan matn kontekstini oʻqish uchun UI Automation |
| Win32 API (P/Invoke) | `SetWindowsHookEx`, `SetWinEventHook`, `SendInput` orqali tizim darajasidagi hook va kiritish |
| Single-file, self-contained publish | Bitta mustaqil `.exe` fayl; maqsadli kompyuterda .NET runtime talab qilinmaydi |

Arxitektura jihatidan dastur oynasiz WinForms `ApplicationContext` sifatida ishlaydi va butun hayot davri tray ikonka orqali boshqariladi. Klaviatura hodisalari `WH_KEYBOARD_LL` low-level hook orqali ushlanadi; `'` yoki `"` bosilganda kontekst aniqlanadi, asl tugma bosilishi bloklanadi va oʻrniga `SendInput` bilan kerakli Unicode belgi yuboriladi.

## Asosiy imkoniyatlar

- **Tizim boʻylab klaviatura hook** — `WH_KEYBOARD_LL` orqali istalgan dastur ichida ishlaydi.
- **UI Automation orqali kontekst aniqlash** — `'` yoki `"` bosilganda kursordan chapdagi haqiqiy belgi `TextPattern` orqali oʻqiladi.
- **Zaxira mexanizm** — UI Automation qoʻllab-quvvatlanmagan dasturlarda oxirgi bosilgan tugmalar tarixi asosida kontekst aniqlanadi.
- **Sichqoncha va oyna kuzatuvi** — sichqoncha bosilganda yoki faol oyna almashganda saqlangan kontekst tozalanadi, bu notoʻgʻri almashtirishning oldini oladi.
- **Sintetik kiritishdan himoya** — dasturning oʻzi yuborgan belgilar `dwExtraInfo` tegi bilan belgilanadi va hook tomonidan qayta ishlanmaydi, shu bois cheksiz aylanish yuzaga kelmaydi.
- **Avtomatik ishga tushirish** — Windows bilan birga ishga tushish `HKCU\...\Run` registr kaliti orqali yoqiladi yoki oʻchiriladi.
- **Yagona nusxa nazorati** — nomlangan `Mutex` orqali bir vaqtning oʻzida faqat bitta nusxa ishlashi taʼminlanadi.
- **Tray ikonka va kontekst menyu** — dastur bildirishnomalar maydonida turadi; menyu orqali avtomatik ishga tushirishni yoqish va dasturdan chiqish mumkin.

## Ishlash mexanizmi

1. `Program.cs` `Mutex` orqali dastur yagona nusxada ishlashini tekshiradi va `TrayAppContext` ni ishga tushiradi.
2. `TrayAppContext` uchta kuzatuvchini ishga tushiradi: klaviatura hook, sichqoncha hook va faol oyna kuzatuvchisi.
3. `'` yoki `"` bosilganda `KeyboardHook` avval `CaretContextReader` orqali kursordan chapdagi belgini aniqlashga urinadi. Bu ishlamasa, ichki kuzatilayotgan oxirgi belgiga qaytadi.
4. Kontekstga qarab kerakli Unicode belgi tanlanadi, asl tugma bloklanadi va belgi `SendInput` bilan yuboriladi.
5. Sichqoncha bosilishi yoki oyna almashishi kontekstni tozalaydi, chunki bunday holatlarda kursor oldidagi belgi haqidagi taxmin eskirgan boʻlishi mumkin.

## Loyiha tuzilishi

```
UzTypist/
├── UzTypist.csproj                       # Loyiha konfiguratsiyasi (.NET 10, single-file publish)
├── README.md                             # Loyiha hujjati
└── src/                                  # Barcha manba kodi
    ├── Program.cs                        # Kirish nuqtasi: Mutex tekshiruvi va ApplicationContext
    ├── Tray/
    │   └── TrayAppContext.cs             # Tray ikonka, kontekst menyu, avtostart (Registry)
    ├── Hooks/                            # Tizim darajasidagi Win32 hook'lar
    │   ├── KeyboardHook.cs               # WH_KEYBOARD_LL: tugmani ushlash, kontekst, SendInput
    │   ├── MouseClickWatcher.cs          # WH_MOUSE_LL: sichqoncha bosilganda kontekstni tozalash
    │   └── ForegroundWindowWatcher.cs    # WinEventHook: oyna almashganda kontekstni tozalash
    └── Context/
        └── CaretContextReader.cs         # UI Automation: kursordan chapdagi belgini oʻqish
```

Manba kodi masʼuliyat boʻyicha papkalarga ajratilgan va har papka alohida namespaceʼga ega: `UzTypist` (kirish nuqtasi), `UzTypist.Tray`, `UzTypist.Hooks` va `UzTypist.Context`. Yangi imkoniyat qoʻshilganda tegishli papkaga fayl qoʻshish yetarli — .NET SDK barcha `.cs` fayllarni avtomatik topgani uchun `.csproj` ni oʻzgartirish talab qilinmaydi.

Modullar orasidagi bogʻliqlik:

```
Program
  └── TrayAppContext
        ├── KeyboardHook
        │     └── CaretContextReader
        ├── MouseClickWatcher
        └── ForegroundWindowWatcher
```

## Qurish va ishga tushirish

Qurish uchun **.NET 10 SDK** talab qilinadi.

```powershell
# Dasturchi rejimida ishga tushirish
dotnet run

# Tarqatish uchun mustaqil .exe fayl yaratish
dotnet publish -c Release
```

Natija bitta faylda yigʻiladi:

```
bin\Release\net10.0-windows\win-x64\publish\UzTypist.exe
```

Bu fayl mustaqil (self-contained) boʻlgani uchun uni Windows 10 yoki 11 kompyuteriga koʻchirib, ikki marta bosish orqali ishga tushirish mumkin; alohida .NET runtime oʻrnatish shart emas.
