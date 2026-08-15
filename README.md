# YT Studio Downloader — Windows 10 x64 — C# WinForms v1

هذه النسخة هي الأساس الصحيح للمشروع:
- C# + .NET 8 + WinForms
- لا Electron ولا Chromium
- لا Node.js/npm
- واجهة غير متجمدة: yt-dlp/FFmpeg يعملان في عمليات منفصلة مع async/await
- Analyze فعلي
- فيديو كامل / مقاطع متعددة / Playlist / Audio
- جودة تُحدَّث بعد التحليل
- MP4/MKV/WEBM وملفات صوتية
- مجلد واسم ملف
- Queue ظاهرة
- Progress وCancel
- System Tray سيضاف في الإصدار التالي.

## البناء على Windows 10
يتطلب .NET 8 SDK أثناء التطوير فقط:

dotnet restore
dotnet build -c Release

EXE مستقل:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

الملف الناتج:
publish\YT Studio Downloader.exe

لا أستطيع اختبار WinForms/Windows من بيئة Linux هنا؛ لذلك لم أزعم أن EXE مبني ومختبر. المشروع كامل المصدر وجاهز للبناء على Windows 10.


## GitHub Actions — بناء EXE تلقائيًا

أضيف إلى المشروع الملف:

`.github/workflows/build-windows10.yml`

بعد رفع المشروع إلى GitHub:

1. افتح تبويب **Actions**.
2. اختر **Build YT Studio Downloader - Windows 10 x64**.
3. اضغط **Run workflow**.
4. بعد نجاح المهمة افتح تشغيل الـ workflow.
5. من قسم **Artifacts** نزّل:
   `YT-Studio-Downloader-Windows10`

سيكون داخله ملف ZIP يحتوي على EXE مستقل لـ Windows x64. GitHub Actions يوفر Windows-hosted runners، و`setup-dotnet` مخصص لتثبيت إصدار .NET المحدد، و`upload-artifact@v4` لحفظ ملف البناء كـ Artifact يمكن تنزيله من صفحة تشغيل الـ workflow. 


## إصلاح تحليل الفيديو
تم إصلاح مشكلة:
`The requested operation requires an element of type 'Number', but the target element has type 'Null'.`
وذلك لأن بعض فيديوهات YouTube قد تعيد `duration: null`. أصبح البرنامج يتعامل معها بأمان ويستخدم 00:00:00 عندما لا تكون المدة متاحة.
