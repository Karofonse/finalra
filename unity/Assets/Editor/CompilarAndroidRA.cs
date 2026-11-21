using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menu "RA > Compilar APK para Android": cambia la plataforma a Android, ajusta lo minimo
/// (orientacion vertical, API minima 29 que exige Vuforia, HTTP permitido) y genera Builds/ProyectoRA.apk.
/// El resultado queda tambien en "ra_build_resultado.txt" en la raiz del proyecto.
/// </summary>
[InitializeOnLoad]
public static class CompilarAndroidRA
{
    const string Disparador = "ra_build_trigger";
    const string Resultado = "ra_build_resultado.txt";
    const string Clave = "ra_build_pendiente";
    const string Salida = "Builds/ProyectoRA.apk";
    static double ultimoSondeo;

    static CompilarAndroidRA()
    {
        EditorApplication.update += Tic;
    }

    [MenuItem("RA/Compilar APK para Android", false, 40)]
    static void DesdeMenu()
    {
        SessionState.SetBool(Clave, true);
    }

    static void Tic()
    {
        double ahora = EditorApplication.timeSinceStartup;
        if (ahora - ultimoSondeo < 1.0) return;
        ultimoSondeo = ahora;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        if (File.Exists(Disparador))
        {
            File.Delete(Disparador);
            SessionState.SetBool(Clave, true);
        }
        if (!SessionState.GetBool(Clave, false)) return;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                SessionState.EraseBool(Clave);
                Escribir("FALLO: el modulo Android Build Support no esta instalado en esta version de Unity.");
                return;
            }
            // Recarga los scripts al terminar; la compilacion sigue en el siguiente Tic
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                SessionState.EraseBool(Clave);
                Escribir("FALLO: no se pudo cambiar la plataforma a Android.");
            }
            return;
        }

        SessionState.EraseBool(Clave);
        Compilar();
    }

    static void Compilar()
    {
        var log = new StringBuilder();
        try
        {
            if ((int)PlayerSettings.Android.minSdkVersion < 29) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.Android.forceInternetPermission = true;
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;

            string[] escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            log.AppendLine("id=" + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                + " minSdk=" + (int)PlayerSettings.Android.minSdkVersion + " escenas=" + string.Join(", ", escenas));
            if (escenas.Length == 0)
            {
                log.AppendLine("FALLO: no hay escenas en Build Settings.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Salida));
            BuildReport informe = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = Salida,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });

            BuildSummary r = informe.summary;
            log.AppendLine("RESULTADO=" + r.result + " errores=" + r.totalErrors + " advertencias=" + r.totalWarnings
                + " tamano=" + (r.totalSize / (1024f * 1024f)).ToString("0.0") + "MB tiempo=" + r.totalTime);
            foreach (BuildStep paso in informe.steps)
                foreach (BuildStepMessage m in paso.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        log.AppendLine("[" + paso.name + "] " + m.content);
        }
        catch (Exception e)
        {
            log.AppendLine("EXCEPCION: " + e);
        }
        finally
        {
            Escribir(log.ToString());
        }
    }

    static void Escribir(string texto)
    {
        File.WriteAllText(Resultado, texto);
        Debug.Log("[RA] Compilacion Android:\n" + texto);
    }
}
