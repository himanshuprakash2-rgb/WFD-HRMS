/**
 * Client-Side Image to WebP Conversion & Compression
 * Antigravity HRMS - WFD HRMS
 */

window.imageCompressor = {
    compressFileInput: async function (fileInputId, maxDimension = 1920, quality = 0.82) {
        const input = document.getElementById(fileInputId);
        if (!input || !input.files || input.files.length === 0) {
            return { success: false, error: "No file selected." };
        }

        const file = input.files[0];
        return await this.compressFile(file, maxDimension, quality);
    },

    compressFile: async function (file, maxDimension = 1920, quality = 0.82) {
        if (!file.type.startsWith("image/")) {
            return {
                success: false,
                error: `Invalid file format (${file.type}). Only image files (JPG, PNG, WebP) are allowed.`
            };
        }

        return new Promise((resolve) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                const img = new Image();
                img.onload = function () {
                    let width = img.width;
                    let height = img.height;

                    if (width > maxDimension || height > maxDimension) {
                        if (width > height) {
                            height = Math.round((height * maxDimension) / width);
                            width = maxDimension;
                        } else {
                            width = Math.round((width * maxDimension) / height);
                            height = maxDimension;
                        }
                    }

                    const canvas = document.createElement("canvas");
                    canvas.width = width;
                    canvas.height = height;

                    const ctx = canvas.getContext("2d");
                    ctx.drawImage(img, 0, 0, width, height);

                    canvas.toBlob(
                        function (blob) {
                            if (!blob) {
                                resolve({ success: false, error: "Canvas to WebP conversion failed." });
                                return;
                            }

                            // 2 MB Check (2097152 bytes)
                            const maxAllowedBytes = 2097152;
                            if (blob.size > maxAllowedBytes) {
                                // Try lower quality
                                canvas.toBlob(
                                    function (fallbackBlob) {
                                        if (!fallbackBlob || fallbackBlob.size > maxAllowedBytes) {
                                            resolve({
                                                success: false,
                                                error: `Image size (${(blob.size / (1024 * 1024)).toFixed(2)} MB) exceeds the 2 MB limit even after compression.`
                                            });
                                            return;
                                        }
                                        readBlobAsResult(fallbackBlob, file.name, resolve);
                                    },
                                    "image/webp",
                                    0.65
                                );
                                return;
                            }

                            readBlobAsResult(blob, file.name, resolve);
                        },
                        "image/webp",
                        quality
                    );
                };

                img.onerror = function () {
                    resolve({ success: false, error: "Failed to load image for compression." });
                };

                img.src = e.target.result;
            };

            reader.onerror = function () {
                resolve({ success: false, error: "Could not read input file." });
            };

            reader.readAsDataURL(file);
        });
    }
};

function readBlobAsResult(blob, originalName, resolve) {
    const reader = new FileReader();
    reader.onloadend = function () {
        const base64Data = reader.result.split(",")[1];
        resolve({
            success: true,
            base64: base64Data,
            mimeType: "image/webp",
            fileSizeBytes: blob.size,
            fileName: originalName.substring(0, originalName.lastIndexOf(".")) + ".webp"
        });
    };
    reader.readAsDataURL(blob);
}
