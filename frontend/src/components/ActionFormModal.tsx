import type { ReactNode } from 'react'
import { Modal } from '@mantine/core'

type Props = {
  opened: boolean
  title: string
  onClose: () => void
  children: ReactNode
}

/**
 * Reusable modal container for create/edit forms triggered from page action menus.
 */
export function ActionFormModal({ opened, title, onClose, children }: Props) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={title}
      centered
      classNames={{
        content: 'cl-action-modal-content',
        header: 'cl-action-modal-header',
        title: 'cl-action-modal-title',
        body: 'cl-action-modal-body'
      }}
      overlayProps={{ className: 'cl-action-modal-overlay' }}
    >
      {children}
    </Modal>
  )
}
