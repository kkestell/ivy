namespace Ivy;

public static class Role
{
    public const string Adapter = "adp";
    public const string Annotator = "ann";
    public const string Arranger = "arr";
    public const string Artist = "art";
    public const string AssociatedName = "asn";
    public const string Author = "aut";
    public const string AuthorInQuotations = "aqt";
    public const string AuthorOfAfterword = "aft";
    public const string AuthorOfIntroduction = "aui";
    public const string BibliographicAntecedent = "ant";
    public const string BookProducer = "bkp";
    public const string Collaborator = "clb";
    public const string Commentator = "cmm";
    public const string Compiler = "com";
    public const string Designer = "dsr";
    public const string Editor = "edt";
    public const string Illustrator = "ill";
    public const string Lyricist = "lyr";
    public const string MetadataContact = "mdc";
    public const string Musician = "mus";
    public const string Narrator = "nrt";
    public const string Other = "oth";
    public const string Photographer = "pht";
    public const string Printer = "prt";
    public const string Redactor = "red";
    public const string Reviewer = "rev";
    public const string Sponsor = "spn";
    public const string ThesisAdvisor = "ths";
    public const string Transcriber = "trc";
    public const string Translator = "trl";

    public static string Parse(string role)
    {
        return role switch
        {
            "adp" => Adapter,
            "ann" => Annotator,
            "arr" => Arranger,
            "art" => Artist,
            "asn" => AssociatedName,
            "aut" => Author,
            "aqt" => AuthorInQuotations,
            "aft" => AuthorOfAfterword,
            "aui" => AuthorOfIntroduction,
            "ant" => BibliographicAntecedent,
            "bkp" => BookProducer,
            "clb" => Collaborator,
            "cmm" => Commentator,
            "com" => Compiler,
            "dsr" => Designer,
            "edt" => Editor,
            "ill" => Illustrator,
            "lyr" => Lyricist,
            "mdc" => MetadataContact,
            "mus" => Musician,
            "nrt" => Narrator,
            "oth" => Other,
            "pht" => Photographer,
            "prt" => Printer,
            "red" => Redactor,
            "rev" => Reviewer,
            "spn" => Sponsor,
            "ths" => ThesisAdvisor,
            "trc" => Transcriber,
            "trl" => Translator,
            _ => Other
        };
    }
}