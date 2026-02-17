import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Card, Checkbox, Select, Stack, Text, TextInput, Title } from '@mantine/core'
import { IconArrowLeft } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { createCategory, listCategories, updateCategory } from '../lib/cozyApi'
import { CategoryType } from '../lib/types'

type Props = {
  token: string
  bookId: string
  onBack: () => void
}

/**
 * Settings sub-page for category management.
 */
export function CategoriesSettingsPage({ token, bookId, onBack }: Props) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [categoryName, setCategoryName] = useState('')
  const [categoryType, setCategoryType] = useState(String(CategoryType.Expense))
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null)

  const categoriesQuery = useQuery({
    queryKey: ['categories', bookId],
    queryFn: () => listCategories(token, bookId)
  })

  const selectedCategory = categoriesQuery.data?.find((category) => category.id === selectedCategoryId)
  const categoryTypeOptions = [
    { label: t('typeExpense'), value: String(CategoryType.Expense) },
    { label: t('typeIncome'), value: String(CategoryType.Income) }
  ]

  const upsertCategory = useMutation({
    mutationFn: async () => {
      const payload = {
        nameEn: categoryName,
        nameZhHans: categoryName,
        type: Number(categoryType) as CategoryType,
        parentId: null,
        isActive: true
      }

      if (selectedCategory) {
        return updateCategory(token, bookId, selectedCategory.id, payload)
      }

      return createCategory(token, bookId, payload)
    },
    onSuccess: async () => {
      setCategoryName('')
      setSelectedCategoryId(null)
      await queryClient.invalidateQueries({ queryKey: ['categories', bookId] })
    }
  })

  const handleCategorySubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!categoryName.trim()) {
      return
    }

    upsertCategory.mutate()
  }

  const getCategoryTypeLabel = (value: CategoryType) =>
    value === CategoryType.Income ? t('typeIncome') : t('typeExpense')

  return (
    <section className="page-panel">
      <div className="page-titlebar">
        <div className="titlebar-leading">
          <Button variant="light" leftSection={<IconArrowLeft size={16} />} onClick={onBack}>
            {t('backButton')}
          </Button>
          <div>
            <Title order={2}>{t('categoriesTitle')}</Title>
            <p>{t('categoriesSettingsHint')}</p>
          </div>
        </div>
      </div>

      <Card shadow="sm" radius="md" className="surface-card">
        <form onSubmit={handleCategorySubmit} className="form-grid">
          <Text fw={600}>{selectedCategory ? t('editCategory') : t('createCategory')}</Text>
          <TextInput value={categoryName} onChange={(event) => setCategoryName(event.currentTarget.value)} label={t('nameLabel')} required />
          <Select
            value={categoryType}
            onChange={(value) => setCategoryType(value ?? String(CategoryType.Expense))}
            data={categoryTypeOptions}
            label={t('typeLabel')}
          />
          <Checkbox label={t('activeLabel')} checked readOnly />
          <Button type="submit" loading={upsertCategory.isPending}>
            {selectedCategory ? t('saveCategory') : t('addCategory')}
          </Button>
        </form>
        <Stack gap="xs" mt="md" className="rows-stack">
          {categoriesQuery.data?.map((category) => (
            <div key={category.id} className="list-row">
              <div className="list-row-main">
                <span className="list-row-title">{category.nameEn}</span>
                <span className="list-row-meta">{getCategoryTypeLabel(category.type)}</span>
              </div>
              <Button
                variant="light"
                size="xs"
                onClick={() => {
                  setSelectedCategoryId(category.id)
                  setCategoryName(category.nameEn)
                  setCategoryType(String(category.type))
                }}
              >
                {t('editButton')}
              </Button>
            </div>
          ))}
        </Stack>
      </Card>
    </section>
  )
}
