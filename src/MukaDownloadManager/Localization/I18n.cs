namespace MukaDownloadManager.Localization;

public static class I18n
{
    public static string Language { get; set; } = "pt-BR";
    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["app.subtitle"] = ["Your downloads. Your control.", "Seus downloads. Seu controle.", "Tus descargas. Tu control."],
        ["nav.all"] = ["All downloads", "Todos os downloads", "Todas las descargas"],
        ["nav.active"] = ["Active", "Em andamento", "En curso"],
        ["nav.completed"] = ["Completed", "Concluídos", "Completadas"],
        ["nav.queued"] = ["Queue", "Fila", "Cola"],
        ["nav.settings"] = ["Settings", "Configurações", "Configuración"],
        ["heading"] = ["Download center", "Central de downloads", "Centro de descargas"],
        ["description"] = ["Fast, clean and completely free.", "Rápido, moderno e totalmente gratuito.", "Rápido, moderno y totalmente gratuito."],
        ["new"] = ["New download", "Novo download", "Nueva descarga"],
        ["start"] = ["Start", "Iniciar", "Iniciar"],
        ["pause"] = ["Pause", "Pausar", "Pausar"],
        ["delete"] = ["Remove", "Remover", "Eliminar"],
        ["open"] = ["Open file", "Abrir arquivo", "Abrir archivo"],
        ["folder"] = ["Open folder", "Abrir pasta", "Abrir carpeta"],
        ["search"] = ["Search downloads...", "Pesquisar downloads...", "Buscar descargas..."],
        ["card.total"] = ["Total downloads", "Total de downloads", "Descargas totales"],
        ["card.active"] = ["Downloading", "Baixando", "Descargando"],
        ["card.done"] = ["Completed", "Concluídos", "Completadas"],
        ["card.speed"] = ["Current speed", "Velocidade atual", "Velocidad actual"],
        ["empty"] = ["No downloads yet. Add a link to get started.", "Nenhum download. Adicione um link para começar.", "Sin descargas. Añade un enlace para comenzar."],
        ["status.queued"] = ["Queued", "Na fila", "En cola"],
        ["status.downloading"] = ["Downloading", "Baixando", "Descargando"],
        ["status.paused"] = ["Paused", "Pausado", "Pausada"],
        ["status.completed"] = ["Completed", "Concluído", "Completada"],
        ["status.failed"] = ["Error", "Erro", "Error"],
        ["dialog.add"] = ["Add a download", "Adicionar download", "Agregar descarga"],
        ["dialog.url"] = ["File URL (HTTP or HTTPS)", "Link do arquivo (HTTP ou HTTPS)", "Enlace del archivo (HTTP o HTTPS)"],
        ["dialog.filename"] = ["File name", "Nome do arquivo", "Nombre del archivo"],
        ["dialog.cancel"] = ["Cancel", "Cancelar", "Cancelar"],
        ["dialog.save"] = ["Add to queue", "Adicionar à fila", "Agregar a la cola"],
        ["dialog.invalid"] = ["Enter a valid HTTP or HTTPS URL.", "Insira um endereço HTTP ou HTTPS válido.", "Introduce una URL HTTP o HTTPS válida."],
        ["settings.title"] = ["Preferences", "Preferências", "Preferencias"],
        ["settings.language"] = ["Language", "Idioma", "Idioma"],
        ["settings.theme"] = ["Appearance", "Aparência", "Apariencia"],
        ["settings.folder"] = ["Downloads folder", "Pasta de downloads", "Carpeta de descargas"],
        ["settings.concurrency"] = ["Concurrent downloads (1–8)", "Downloads simultâneos (1–8)", "Descargas simultáneas (1–8)"],
        ["settings.segments"] = ["Connections per file (1–16)", "Conexões por arquivo (1–16)", "Conexiones por archivo (1–16)"],
        ["settings.speed"] = ["Speed limit in KiB/s (0 = unlimited)", "Limite de velocidade em KiB/s (0 = ilimitado)", "Límite de velocidad KiB/s (0 = ilimitado)"],
        ["settings.auto"] = ["Start queued downloads automatically", "Iniciar downloads da fila automaticamente", "Iniciar descargas de la cola automáticamente"],
        ["settings.clipboard"] = ["Detect HTTP links copied to clipboard", "Detectar links HTTP copiados", "Detectar enlaces HTTP copiados"],
        ["settings.save"] = ["Save preferences", "Salvar preferências", "Guardar preferencias"],
        ["confirm.remove"] = ["Remove selected download from the list? Partial files are preserved.", "Remover o download selecionado da lista? Arquivos parciais serão mantidos.", "¿Eliminar la descarga seleccionada? Se conservarán los archivos parciales."],
        ["error"] = ["Error", "Erro", "Error"],
        ["select"] = ["Select a download first.", "Selecione um download.", "Selecciona una descarga."],
    };
    public static string T(string key)
    {
        if (!Texts.TryGetValue(key, out var options)) return key;
        return options[Language switch { "en" => 0, "es" => 2, _ => 1 }];
    }
}
