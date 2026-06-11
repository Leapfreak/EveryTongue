Namespace Models

    Public Class BenchmarkRun
        Public Property Id As String = ""
        Public Property Timestamp As DateTime = DateTime.Now
        Public Property Backend As String = ""
        Public Property AudioFile As String = ""
        Public Property ReferenceFile As String = ""
        Public Property Language As String = ""
        Public Property Pipeline As String = ""
        Public Property Notes As String = ""

        ' Parameters
        Public Property BeamSize As Integer = 5
        Public Property BestOf As Integer = 1
        Public Property Temperature As Double = 0.0
        Public Property InitialPrompt As String = ""
        Public Property VadMinSilenceMs As Integer = 750
        Public Property VadMaxSegmentS As Integer = 25
        Public Property InterimIntervalMs As Integer = 1000
        Public Property RealtimeFactor As Double = 0.0

        ' Results
        Public Property Wer As Double = 0.0
        Public Property Substitutions As Integer = 0
        Public Property Deletions As Integer = 0
        Public Property Insertions As Integer = 0
        Public Property RefWords As Integer = 0
        Public Property HypWords As Integer = 0
        Public Property CommitCount As Integer = 0
        Public Property HallucinationCount As Integer = 0
        Public Property AudioDurationS As Double = 0.0
        Public Property ElapsedS As Double = 0.0

        ' Full texts
        Public Property OutputText As String = ""
        Public Property StatsSummary As String = ""
    End Class

    Public Class WerResult
        Public Property Wer As Double = 0.0
        Public Property Substitutions As Integer = 0
        Public Property Deletions As Integer = 0
        Public Property Insertions As Integer = 0
        Public Property RefWords As Integer = 0
        Public Property HypWords As Integer = 0
    End Class

End Namespace
