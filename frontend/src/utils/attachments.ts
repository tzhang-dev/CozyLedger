import imageCompression from 'browser-image-compression';

const MAX_IMAGE_BYTES = 1024 * 1024;
const MAX_IMAGE_MB = MAX_IMAGE_BYTES / (1024 * 1024);

export const allowedAttachmentTypes = ['image/jpeg', 'image/png', 'application/pdf'] as const;

export type PreparedAttachment = {
  file: File;
  originalSizeBytes: number;
};

export async function prepareAttachment(file: File): Promise<PreparedAttachment> {
  if (!file.type.startsWith('image/')) {
    return { file, originalSizeBytes: file.size };
  }

  if (file.size <= MAX_IMAGE_BYTES) {
    return { file, originalSizeBytes: file.size };
  }

  const compressed = await imageCompression(file, {
    maxSizeMB: MAX_IMAGE_MB,
    maxWidthOrHeight: 2000,
    useWebWorker: true,
  });

  return {
    file: compressed,
    originalSizeBytes: file.size,
  };
}