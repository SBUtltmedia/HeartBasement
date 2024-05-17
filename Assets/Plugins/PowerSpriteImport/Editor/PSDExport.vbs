rem =======================================================
rem Created by Dave Lloyd (@duzzondrums) for Powerhoof - http://tools.powerhoof.com for updates

On Error Resume Next

DIM photoshop, doc, currentDir, exportPath, sourcePath, dialogMode, documentState, filename


rem =======================================================
rem INITIAL SETUP

set shell = CreateObject("WScript.Shell")
set fileSystem = CreateObject("Scripting.FileSystemObject") 
set photoshop = CreateObject("Photoshop.Application")
set args = Wscript.Arguments

dialogMode = 3

rem =======================================================
rem OPEN SOURCE DOC

if ( args.Count > 0 and args(0) <> "active"  ) then
	rem First argument is path to source file, unless it's "active", then it'll use the active one
	sourcePath = args(0) 
	set document = photoshop.Open(sourcePath)
	rem WScript.Echo sourcePath
elseif photoshop.Documents.Count = 0 then
	rem No document open, bail
	Wscript.Echo "You need to have a file open in Photoshop, or drag the PSD you want to export over this VBS file"
	Wscript.Quit
else 
	rem If no args- export active document
	set document = photoshop.ActiveDocument
end if

rem =======================================================
rem SET UP EXPORT PATH

if ( args.Count > 1 ) then
	rem Second arg is path to export folder
	exportPath = args(1)
else
	rem If no export folder specified, create one in curr folder
	currentDir = replace( WScript.ScriptFullName, WScript.ScriptName, "" ) 
	exportPath = currentDir & "Export"
	rem Delete existing export files
	fileSystem.DeleteFolder exportPath
end if

rem Create folder if it doesn't exist yet
fileSystem.CreateFolder exportPath

rem WScript.Echo exportPath

rem =======================================================
rem WAIT FOR DOCUMENT TO OPEN (KEEP SLEEPING UNTIL DOC COUNT ISN'T ZERO... (AND AT LEAST ONCE)

dim docCount, sleepCount
docCount = 0
sleepCount = 0
while docCount = 0 and sleepCount < 600	rem Max wait: 2 mins
	WScript.Sleep 200	
	docCount = photoshop.Documents.Count
	sleepCount= sleepCount +1
wend

if ( sleepCount > 599 ) then
	Wscript.Quit
end if

rem =======================================================
rem STORE DOCUMENT STATE

set documentState = document.ActiveHistoryState

rem =======================================================
rem CREATE FILENAME based on document's name

filename = document.Name
filename = left(filename, len(filename)-4)

rem =======================================================
rem EXPORT AS SINGLE PNG FIRST, in case there's only 1 frame (which photoshop won't render as video)

DIM idsave
idsave = photoshop.CharIDToTypeID( "save" )
    DIM desc56
    SET desc56 = CreateObject( "Photoshop.ActionDescriptor" )
    DIM idAs1
    idAs1 = photoshop.CharIDToTypeID( "As  " )
        DIM desc57
        SET desc57 = CreateObject( "Photoshop.ActionDescriptor" )
        DIM idMthd
        idMthd = photoshop.CharIDToTypeID( "Mthd" )
        DIM idPNGMethod
        idPNGMethod = photoshop.StringIDToTypeID( "PNGMethod" )
        DIM idthorough
        idthorough = photoshop.StringIDToTypeID( "thorough" )
        Call desc57.PutEnumerated( idMthd, idPNGMethod, idthorough )
    DIM idPNGF1
    idPNGF1 = photoshop.CharIDToTypeID( "PNGF" )
    Call desc56.PutObject( idAs1, idPNGF1, desc57 )
    DIM idIn
    idIn = photoshop.CharIDToTypeID( "In  " )
    Call desc56.PutPath( idIn, exportPath & "\" & filename & "_1.png" )
    DIM idDocI
    idDocI = photoshop.CharIDToTypeID( "DocI" )
    Call desc56.PutInteger( idDocI, 195 )
    DIM idCpy
    idCpy = photoshop.CharIDToTypeID( "Cpy " )
    Call desc56.PutBoolean( idCpy, True )
    DIM idsaveStage
    idsaveStage = photoshop.StringIDToTypeID( "saveStage" )
    DIM idsaveStageType
    idsaveStageType = photoshop.StringIDToTypeID( "saveStageType" )
    DIM idsaveBegin
    idsaveBegin = photoshop.StringIDToTypeID( "saveBegin" )
    Call desc56.PutEnumerated( idsaveStage, idsaveStageType, idsaveBegin )
Call photoshop.ExecuteAction( idsave, desc56, dialogMode )

rem Save succeeded action- Most stuff in the action descriptor is copied from previous, but with different save success
idsave = photoshop.CharIDToTypeID( "save" )
    idsaveSucceeded = photoshop.StringIDToTypeID( "saveSucceeded" )
    Call desc56.PutEnumerated( idsaveStage, idsaveStageType, idsaveSucceeded )
Call photoshop.ExecuteAction( idsave, desc56, dialogMode )

rem =======================================================
rem SELECT ALL FRAMES

dim idanimationSelectAll
idanimationSelectAll = photoshop.StringIDToTypeID( "animationSelectAll" )
    dim desc92
    set desc92 = CreateObject( "Photoshop.ActionDescriptor" )
call photoshop.ExecuteAction( idanimationSelectAll, desc92, dialogMode )

rem =======================================================
rem set FRAME DURATIONS TO 1 SECOND

dim idsetd
idsetd = photoshop.CharIDToTypeID( "setd" )
    dim desc93
    set desc93 = CreateObject( "Photoshop.ActionDescriptor" )
    dim idnull
    dim idanimationFrameClass
    idnull = photoshop.CharIDToTypeID( "null" )
        dim ref33
        set ref33 = CreateObject( "Photoshop.ActionReference" )
        idanimationFrameClass = photoshop.StringIDToTypeID( "animationFrameClass" )
        dim idOrdn
        idOrdn = photoshop.CharIDToTypeID( "Ordn" )
        dim idTrgt
        idTrgt = photoshop.CharIDToTypeID( "Trgt" )
        call ref33.PutEnumerated( idanimationFrameClass, idOrdn, idTrgt )
    call desc93.PutReference( idnull, ref33 )
    dim idT
    idT = photoshop.CharIDToTypeID( "T   " )
        dim desc94
        set desc94 = CreateObject( "Photoshop.ActionDescriptor" )
        dim idanimationFrameDelay
        idanimationFrameDelay = photoshop.StringIDToTypeID( "animationFrameDelay" )
        call desc94.PutDouble( idanimationFrameDelay, 1.000000 )
    idanimationFrameClass = photoshop.StringIDToTypeID( "animationFrameClass" )
    call desc93.PutObject( idT, idanimationFrameClass, desc94 )
call photoshop.ExecuteAction( idsetd, desc93, dialogMode )

rem =======================================================
rem EXPORT FILES AS VIDEO

idExpr = photoshop.CharIDToTypeID( "Expr" )
    dim desc102
    set desc102 = CreateObject( "Photoshop.ActionDescriptor" )
    dim idUsng
    idUsng = photoshop.CharIDToTypeID( "Usng" )
        dim desc103
        set desc103 = CreateObject( "Photoshop.ActionDescriptor" )
        dim iddirectory
        iddirectory = photoshop.StringIDToTypeID( "directory" )
        call desc103.PutPath( iddirectory, exportPath )
        dim idNm
        idNm = photoshop.CharIDToTypeID( "Nm  " )
		rem set FILENAME
        call desc103.PutString( idNm, filename & "_.png" )
        dim idsequenceRenderSettings
        idsequenceRenderSettings = photoshop.StringIDToTypeID( "sequenceRenderSettings" )
            dim desc104
            set desc104 = CreateObject( "Photoshop.ActionDescriptor" )
            dim idPNGF
            idPNGF = photoshop.CharIDToTypeID( "PNGF" )
            dim idAs
            idAs = photoshop.CharIDToTypeID( "As  " )
                dim desc105
                set desc105 = CreateObject( "Photoshop.ActionDescriptor" )
                dim idPGIT
                idPGIT = photoshop.CharIDToTypeID( "PGIT" )
                dim idPGIN
                idPGIN = photoshop.CharIDToTypeID( "PGIN" )
                call desc105.PutEnumerated( idPGIT, idPGIT, idPGIN )
                dim idPGAd
                idPGAd = photoshop.CharIDToTypeID( "PGAd" )
                call desc105.PutEnumerated( idPNGf, idPNGf, idPGAd )
                dim idCmpr
                idCmpr = photoshop.CharIDToTypeID( "Cmpr" )
                call desc105.PutInteger( idCmpr, 0 )
            call desc104.PutObject( idAs, idPNGF, desc105 )
        call desc103.PutObject( idsequenceRenderSettings, idsequenceRenderSettings, desc104 )
		rem Set frame id padding for filename
        dim idminDigits
        idminDigits = photoshop.StringIDToTypeID( "minDigits" )
        call desc103.PutInteger( idminDigits, 1 )
		rem Start at number 1
        dim idstartNumber
        idstartNumber = photoshop.StringIDToTypeID( "startNumber" )
        call desc103.PutInteger( idstartNumber, 0 )
        dim iduseDocumentSize
        iduseDocumentSize = photoshop.StringIDToTypeID( "useDocumentSize" )
        call desc103.PutBoolean( iduseDocumentSize, True )		
		rem Set framerate
        dim idframeRate
        idframeRate = photoshop.StringIDToTypeID( "frameRate" )
        call desc103.PutDouble( idframeRate, 1.000000 )
		rem Render all frames
        dim idallFrames
        idallFrames = photoshop.StringIDToTypeID( "allFrames" )
        call desc103.PutBoolean( idallFrames, True )
        dim idrenderAlpha
        idrenderAlpha = photoshop.StringIDToTypeID( "renderAlpha" )
        dim idalphaRendering
        idalphaRendering = photoshop.StringIDToTypeID( "alphaRendering" )
		rem Set alpha premultiply settings. Not sure which is correct tbh...
        dim idpremultiplyWhite
        idpremultiplyWhite = photoshop.StringIDToTypeID( "premultiplyWhite" )
        call desc103.PutEnumerated( idrenderAlpha, idalphaRendering, idpremultiplyWhite )		
		rem Set quality settings
        dim idQlty
        idQlty = photoshop.CharIDToTypeID( "Qlty" )
        call desc103.PutInteger( idQlty, 1 )
        dim idZthreeDPrefHighQualityErrorThreshold
        idZthreeDPrefHighQualityErrorThreshold = photoshop.StringIDToTypeID( "Z3DPrefHighQualityErrorThreshold" )
        call desc103.PutInteger( idZthreeDPrefHighQualityErrorThreshold, 5 )
    dim idvideoExport
    idvideoExport = photoshop.StringIDToTypeID( "videoExport" )
    call desc102.PutObject( idUsng, idvideoExport, desc103 )
rem Do the export
call photoshop.ExecuteAction( idExpr, desc102, dialogMode )


rem =======================================================
rem RESTORE DOCUMENT STATE

set photoshop.ActiveDocument.ActiveHistoryState = documentState

rem =======================================================

rem shell.Popup "Exporting from psp, please wait.", , "Exporting..."

