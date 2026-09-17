// ViewLocator.cs — Traduce un ViewModel a su vista por convención de NOMBRES (sufijo ViewModel→View); núcleo del DataBinding MVVM de Avalonia.
using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation;

[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        // Convención de nombres obligatoria: la vista debe llamarse igual que el VM
        // cambiando el sufijo "ViewModel" → "View" (p. ej. MantenimientoViewModel →
        // MantenimientoView). Si no respetas ese nombre, aquí se muestra "Not Found".
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
