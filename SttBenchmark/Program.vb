Imports System.Windows.Forms

Namespace Global.SttBenchmark
    Module Program
        <STAThread()>
        Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New FormMain())
        End Sub
    End Module
End Namespace
