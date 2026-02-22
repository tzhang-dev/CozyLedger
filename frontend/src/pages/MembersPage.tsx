import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Button, Group, TextInput } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { acceptInvite, createInvite } from '../lib/cozyApi'
import { saveSession } from '../lib/session'

type Props = {
  token: string
  bookId: string
  onBookJoined: (bookId: string) => void
}

/**
 * Handles invite generation and invite acceptance for book membership.
 */
export function MembersPage({ token, bookId, onBookJoined }: Props) {
  const { t } = useTranslation()
  const [inviteToken, setInviteToken] = useState('')

  const createInviteMutation = useMutation({
    mutationFn: () => createInvite(token, bookId)
  })

  const acceptInviteMutation = useMutation({
    mutationFn: () => acceptInvite(token, inviteToken),
    onSuccess: (result) => {
      saveSession({ token, bookId: result.bookId })
      onBookJoined(result.bookId)
      setInviteToken('')
    }
  })

  const handleAccept = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!inviteToken.trim()) {
      return
    }

    acceptInviteMutation.mutate()
  }

  return (
    <section className="cl-page">
      <header className="cl-header">
        <div className="cl-header-inner">
          <h1 className="cl-header-title">{t('membersTitle')}</h1>
          <p className="cl-header-subtitle">{t('membersSubtitle')}</p>
        </div>
      </header>

      <div className="cl-body">
        <div className="cl-card cl-form-grid">
          <p className="cl-card-title">{t('membersGenerateTitle')}</p>
          <Button onClick={() => createInviteMutation.mutate()} loading={createInviteMutation.isPending}>
            {t('createInviteLink')}
          </Button>
          {createInviteMutation.data ? (
            <div className="cl-list">
              <div className="cl-list-row">
                <span className="cl-list-row-main">
                  <span className="cl-list-row-title">{t('inviteTokenLabel')}</span>
                  <span className="cl-list-row-meta">{createInviteMutation.data.token}</span>
                </span>
              </div>
              <div className="cl-list-row">
                <span className="cl-list-row-main">
                  <span className="cl-list-row-title">{t('inviteUrlLabel')}</span>
                  <span className="cl-list-row-meta">{createInviteMutation.data.inviteUrl}</span>
                </span>
              </div>
            </div>
          ) : null}
        </div>

        <div className="cl-card">
          <form onSubmit={handleAccept} className="cl-form-grid">
            <p className="cl-card-title">{t('membersAcceptTitle')}</p>
            <TextInput value={inviteToken} onChange={(event) => setInviteToken(event.currentTarget.value)} label={t('inviteTokenInput')} required />
            <Group>
              <Button type="submit" loading={acceptInviteMutation.isPending}>
                {t('joinBook')}
              </Button>
            </Group>
          </form>
        </div>
      </div>
    </section>
  )
}
