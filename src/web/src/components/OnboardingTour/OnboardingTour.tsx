import { useEffect, useMemo, useState } from 'react'
import type { OnboardingStep } from '../../onboarding/roleTours'
import styles from './OnboardingTour.module.css'

interface OnboardingTourProps {
  isOpen: boolean
  title: string
  steps: OnboardingStep[]
  onClose: () => void
}

interface HighlightRect {
  top: number
  left: number
  width: number
  height: number
}

function clampStep(value: number, length: number): number {
  if (length === 0) return 0
  if (value < 0) return 0
  if (value > length - 1) return length - 1
  return value
}

export function OnboardingTour({ isOpen, title, steps, onClose }: OnboardingTourProps) {
  const [activeIndex, setActiveIndex] = useState(0)
  const [highlightRect, setHighlightRect] = useState<HighlightRect | null>(null)

  useEffect(() => {
    if (isOpen) {
      setActiveIndex(0)
    }
  }, [isOpen])

  const activeStep = useMemo(
    () => steps[clampStep(activeIndex, steps.length)],
    [activeIndex, steps],
  )

  useEffect(() => {
    if (!isOpen || !activeStep) {
      setHighlightRect(null)
      return
    }

    function updateHighlight() {
      const element = document.querySelector(activeStep.target)
      if (!(element instanceof HTMLElement)) {
        setHighlightRect(null)
        return
      }

      const rect = element.getBoundingClientRect()
      const padding = 6
      setHighlightRect({
        top: Math.max(rect.top - padding, 0),
        left: Math.max(rect.left - padding, 0),
        width: Math.min(rect.width + (padding * 2), window.innerWidth),
        height: Math.min(rect.height + (padding * 2), window.innerHeight),
      })
    }

    updateHighlight()
    const element = document.querySelector(activeStep.target)
    if (element instanceof HTMLElement && typeof element.scrollIntoView === 'function') {
      element.scrollIntoView({ block: 'center', behavior: 'smooth' })
    }

    window.addEventListener('resize', updateHighlight)
    window.addEventListener('scroll', updateHighlight, true)
    return () => {
      window.removeEventListener('resize', updateHighlight)
      window.removeEventListener('scroll', updateHighlight, true)
    }
  }, [activeStep, isOpen])

  if (!isOpen || !activeStep) {
    return null
  }

  const isLastStep = activeIndex === steps.length - 1

  return (
    <div className={styles.root} role="dialog" aria-modal="false" aria-label={title}>
      <div className={styles.backdrop} aria-hidden="true" />
      {highlightRect && (
        <div
          className={styles.highlight}
          style={{
            top: `${highlightRect.top}px`,
            left: `${highlightRect.left}px`,
            width: `${highlightRect.width}px`,
            height: `${highlightRect.height}px`,
          }}
          aria-hidden="true"
        />
      )}

      <aside className={styles.panel} data-testid="onboarding-tour">
        <p className={styles.kicker}>{title}</p>
        <h2 className={styles.stepTitle}>{activeStep.title}</h2>
        <p className={styles.stepDescription}>{activeStep.description}</p>

        <p className={styles.progress}>
          Step {activeIndex + 1} of {steps.length}
        </p>

        <div className={styles.actions}>
          <button type="button" className={styles.ghostBtn} onClick={onClose}>
            Skip tour
          </button>

          <div className={styles.primaryActions}>
            <button
              type="button"
              className={styles.ghostBtn}
              onClick={() => setActiveIndex(prev => clampStep(prev - 1, steps.length))}
              disabled={activeIndex === 0}
            >
              Back
            </button>

            <button
              type="button"
              className={styles.primaryBtn}
              onClick={() => {
                if (isLastStep) {
                  onClose()
                  return
                }
                setActiveIndex(prev => clampStep(prev + 1, steps.length))
              }}
            >
              {isLastStep ? 'Finish' : 'Next'}
            </button>
          </div>
        </div>
      </aside>
    </div>
  )
}
