using System.ComponentModel;
using YTStudioDownloader.Models;
using YTStudioDownloader.Services;

namespace YTStudioDownloader.UI;

public sealed class MainForm : Form
{
    readonly ToolManager tools = new();
    readonly YtDlpService ytdlp;
    VideoInfo? info;

    TextBox url = new(), folder = new(), name = new(), log = new();
    ComboBox mode = new(), quality = new(), format = new();
    Label title = new(), meta = new(), toolState = new(), status = new();
    PictureBox thumb = new();
    DataGridView clips = new(), queue = new();
    Button analyze = new(), download = new(), cancel = new();
    ProgressBar progress = new();
    CancellationTokenSource? cts;

    readonly List<DownloadJob> jobs = new();

    public MainForm()
    {
        ytdlp = new YtDlpService(tools);
        Text = "YT Studio Downloader";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1180, 780);
        MinimumSize = new Size(1000, 680);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(15,23,42);
        ForeColor = Color.White;

        Build();
        RefreshTools();
    }

    void Build()
    {
        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=2, Padding=new Padding(16) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var head = new Panel { Dock=DockStyle.Fill };
        head.Controls.Add(new Label { Text="YT Studio Downloader", AutoSize=true, Font=new Font("Segoe UI Semibold",22,FontStyle.Bold) });
        toolState = new Label { Text="فحص الأدوات...", AutoSize=true, ForeColor=Color.Silver, Location=new Point(2,36) };
        head.Controls.Add(toolState);
        root.Controls.Add(head,0,0); root.SetColumnSpan(head,2);

        root.Controls.Add(BuildLeft(),0,1);
        root.Controls.Add(BuildRight(),1,1);

        analyze.Click += async (_,_) => await Analyze();
        download.Click += async (_,_) => await StartDownload();
        cancel.Click += (_,_) => cts?.Cancel();
        mode.SelectedIndexChanged += (_,_) => ToggleClips();
    }

    Control BuildLeft()
    {
        var card=Card();
        var p=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true};
        card.Controls.Add(p);

        AddLabel(p,"رابط الفيديو أو قائمة التشغيل");
        var r=new TableLayoutPanel{Dock=DockStyle.Top,Height=40,ColumnCount=2};
        r.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); r.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));
        url=Box(); analyze=Btn("تحليل الرابط",Color.FromArgb(37,99,235));
        r.Controls.Add(url,0,0); r.Controls.Add(analyze,1,0); p.Controls.Add(r);

        var prev=new TableLayoutPanel{Dock=DockStyle.Top,Height=110,ColumnCount=2};
        prev.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160)); prev.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        thumb=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(30,41,59)};
        var ip=new Panel{Dock=DockStyle.Fill};
        title=new Label{AutoSize=true,Font=new Font("Segoe UI Semibold",13,FontStyle.Bold)};
        meta=new Label{AutoSize=true,ForeColor=Color.Silver,Location=new Point(0,34)};
        ip.Controls.Add(title); ip.Controls.Add(meta);
        prev.Controls.Add(thumb,0,0); prev.Controls.Add(ip,1,0); p.Controls.Add(prev);

        var opts=new TableLayoutPanel{Dock=DockStyle.Top,Height=70,ColumnCount=3};
        opts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.3f));
        opts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.3f));
        opts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.4f));
        mode=Combo("فيديو كامل","مقاطع متعددة","قائمة تشغيل","صوت فقط");
        quality=Combo("best","2160","1440","1080","720","480","360");
        format=Combo("mp4","mkv","webm","mp3","m4a","wav","flac");
        opts.Controls.Add(Labeled("نوع التحميل",mode),0,0);
        opts.Controls.Add(Labeled("الجودة",quality),1,0);
        opts.Controls.Add(Labeled("الصيغة",format),2,0);
        p.Controls.Add(opts);

        AddLabel(p,"مجلد الحفظ"); folder=Box(); folder.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),"YT Studio Downloader"); p.Controls.Add(folder);
        AddLabel(p,"اسم الملف"); name=Box(); name.Text="%(title)s"; p.Controls.Add(name);

        clips=new DataGridView{Dock=DockStyle.Top,Height=180,Visible=false,AllowUserToAddRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,BackgroundColor=Color.FromArgb(15,23,42),ForeColor=Color.White};
        clips.Columns.Add("Start","البداية"); clips.Columns.Add("End","النهاية"); clips.Columns.Add("Name","الاسم");
        p.Controls.Add(clips);

        var addClip=Btn("+ إضافة مقطع",Color.FromArgb(51,65,85)); addClip.Visible=false;
        addClip.Click += (_,_)=>clips.Rows.Add("00:00:00","00:00:30",$"Clip {clips.Rows.Count+1}");
        p.Controls.Add(addClip);

        var ar=new TableLayoutPanel{Dock=DockStyle.Top,Height=42,ColumnCount=2};
        ar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,75)); ar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
        download=Btn("بدء التحميل",Color.FromArgb(37,99,235)); cancel=Btn("إلغاء",Color.FromArgb(153,27,27));
        ar.Controls.Add(download,0,0); ar.Controls.Add(cancel,1,0); p.Controls.Add(ar);

        progress=new ProgressBar{Dock=DockStyle.Top,Height=18}; p.Controls.Add(progress);
        status=new Label{Text="جاهز",AutoSize=true,ForeColor=Color.Silver}; p.Controls.Add(status);

        return card;
    }

    Control BuildRight()
    {
        var card=Card(); var p=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};
        p.RowStyles.Add(new RowStyle(SizeType.Percent,52)); p.RowStyles.Add(new RowStyle(SizeType.Percent,48));
        queue=new DataGridView{Dock=DockStyle.Fill,AutoGenerateColumns=false,ReadOnly=true,AllowUserToAddRows=false,RowHeadersVisible=false,BackgroundColor=Color.FromArgb(15,23,42),ForeColor=Color.White,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
        queue.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="الملف",DataPropertyName="FileName"});
        queue.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="الحالة",DataPropertyName="State"});
        queue.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="%",DataPropertyName="Progress"});
        log=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Color.FromArgb(2,6,23),ForeColor=Color.Gainsboro};
        p.Controls.Add(queue,0,0); p.Controls.Add(log,0,1); card.Controls.Add(p); return card;
    }

    async Task Analyze()
    {
        if(string.IsNullOrWhiteSpace(url.Text)){MessageBox.Show("أدخل الرابط.");return;}
        try{
            Toggle(true); status.Text="جاري التحليل..."; Log("بدء التحليل...");
            info=await ytdlp.AnalyzeAsync(url.Text.Trim(),new Progress<string>(Log),CancellationToken.None);
            title.Text=info.Title; meta.Text=$"{info.Channel} • {TimeSpan.FromSeconds(info.Duration):hh\\:mm\\:ss}";
            quality.Items.Clear(); quality.Items.Add("best"); foreach(var h in info.Heights) quality.Items.Add(h.ToString()); quality.SelectedIndex=0;
            if(!string.IsNullOrWhiteSpace(info.Thumbnail)) await LoadThumb(info.Thumbnail);
            status.Text="تم التحليل."; Log("✓ تم التحليل.");
        }catch(Exception ex){status.Text="فشل التحليل."; Log(ex.Message); MessageBox.Show(ex.Message);}
        finally{Toggle(false);}
    }

    async Task StartDownload()
    {
        if(string.IsNullOrWhiteSpace(url.Text)){MessageBox.Show("أدخل الرابط.");return;}
        var m=(DownloadMode)mode.SelectedIndex;
        var c=new List<ClipRange>();
        if(m==DownloadMode.Clips){
            foreach(DataGridViewRow row in clips.Rows)
                c.Add(new ClipRange{Start=$"{row.Cells[0].Value}",End=$"{row.Cells[1].Value}",Name=$"{row.Cells[2].Value}"});
            if(c.Count==0){MessageBox.Show("أضف مقطعًا.");return;}
        }
        var job=new DownloadJob{
            Url=url.Text.Trim(),Mode=m,Quality=quality.SelectedItem?.ToString()??"best",Format=format.SelectedItem?.ToString()??"mp4",
            Folder=folder.Text.Trim(),FileName=name.Text.Trim(),Clips=c
        };
        Directory.CreateDirectory(job.Folder);
        jobs.Add(job); RefreshQueue(); _=RunJob(job);
    }

    async Task RunJob(DownloadJob job)
    {
        job.State=JobState.Running; RefreshQueue(); cts=new CancellationTokenSource();
        try{
            var pr=new Progress<double>(x=>{progress.Value=(int)Math.Clamp(x,0,100);job.Progress=x;RefreshQueue();});
            var lg=new Progress<string>(Log);
            await ytdlp.DownloadAsync(job,pr,lg,cts.Token);
            job.State=JobState.Completed; job.Progress=100; status.Text="اكتمل التحميل.";
        }catch(OperationCanceledException){job.State=JobState.Cancelled;status.Text="تم الإلغاء.";}
        catch(Exception ex){job.State=JobState.Failed;job.LastError=ex.Message;status.Text="فشل التحميل.";Log(ex.Message);MessageBox.Show(ex.Message);}
        finally{RefreshQueue();cts.Dispose();cts=null;}
    }

    void RefreshQueue(){queue.DataSource=null; queue.DataSource=new BindingList<DownloadJob>(jobs);}
    void ToggleClips()
    {
        bool on=mode.SelectedIndex==1;
        clips.Visible=on;
        if(on && clips.Rows.Count==0) clips.Rows.Add("00:00:00","00:00:30","Clip 1");
        foreach(Control c in Controls)
        {
            // handled by container layout; nothing else needed here.
        }
    }

    async Task LoadThumb(string u)
    {
        try{using var hc=new HttpClient();var b=await hc.GetByteArrayAsync(u);using var ms=new MemoryStream(b);thumb.Image=Image.FromStream(ms);}
        catch{thumb.Image=null;}
    }

    void RefreshTools(){toolState.Text=$"yt-dlp: {(tools.YtDlp!=null?"✓":"✗")}    FFmpeg: {(tools.Ffmpeg!=null?"✓":"✗")}";}
    void Toggle(bool busy){analyze.Enabled=!busy;url.Enabled=!busy;download.Enabled=!busy;}
    void Log(string s){log.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}{Environment.NewLine}");log.SelectionStart=log.TextLength;log.ScrollToCaret();}
    static TextBox Box()=>new(){Dock=DockStyle.Fill};
    static ComboBox Combo(params string[] x){var c=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};c.Items.AddRange(x);c.SelectedIndex=0;return c;}
    static Control Labeled(string t,Control c){var p=new Panel{Dock=DockStyle.Fill};p.Controls.Add(c);p.Controls.Add(new Label{Text=t,Dock=DockStyle.Top,Height=22,ForeColor=Color.Gainsboro});return p;}
    static Button Btn(string t,Color c)=>new(){Text=t,Dock=DockStyle.Fill,BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
    static Panel Card()=>new(){Dock=DockStyle.Fill,Padding=new Padding(12),BackColor=Color.FromArgb(17,24,39),Margin=new Padding(6)};
    static void AddLabel(TableLayoutPanel p,string t)=>p.Controls.Add(new Label{Text=t,AutoSize=true,ForeColor=Color.Gainsboro,Padding=new Padding(0,6,0,4)});

    protected override void OnLoad(EventArgs e){base.OnLoad(e);RefreshTools();}
}
