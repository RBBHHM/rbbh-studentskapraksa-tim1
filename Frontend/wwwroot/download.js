// Application file delivery only. Authorization, generation and token handling
// stay server-side; this module receives only the authorized file bytes.
export async function downloadFile(name, type, reference) {
    const bytes = await reference.arrayBuffer();
    const url = URL.createObjectURL(new Blob([bytes], { type }));
    const link = document.createElement("a");
    try {
        link.href = url;
        link.download = name;
        document.body.appendChild(link);
        link.click();
    } finally {
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
}
