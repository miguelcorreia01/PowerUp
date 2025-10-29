import { Component, computed, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { startWith, switchMap, map, BehaviorSubject, combineLatest, firstValueFrom } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { PersonalTrainingService, Instructor, PtSession } from '../../services/personal-training.service';
import { Nav } from "../../../../shared/components/nav/nav";

type SlotState = 'available' | 'booked' | 'mine';

@Component({
  selector: 'app-personal-training',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, Nav],
  templateUrl: './personal-training.html',
  styleUrls: ['./personal-training.css'],
})
export class PersonalTraining {
  private auth = inject(AuthService);
  private api = inject(PersonalTrainingService);

  role = signal(this.auth.getRole() ?? 'User');
  isInstructor = computed(() => this.role() === 'Instructor');
  userId = signal(((this.auth as any).currentUserSubject?.value?.userId) ?? '');
  
  // Week navigation
  weekStart = signal(startOfWeek(new Date()));

  private maxWeekStart = computed(() => addWeeks(startOfWeek(new Date()), 4)); 
  canGoNext = computed(() => this.weekStart().getTime() < this.maxWeekStart().getTime());
  canGoPrev = computed(() => {
    const currentWeekStart = startOfWeek(new Date());
    return this.weekStart().getTime() > currentWeekStart.getTime();
  });

  hours = signal<number[]>([7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22]);
  days = signal<number[]>([0,1,2,3,4,5,6]);

  // Instructors dropdown
  instructors$ = this.api.getInstructors().pipe(startWith([] as Instructor[]));
  instructors = toSignal(this.instructors$, { initialValue: [] as Instructor[] });
  selectedInstructorId = signal<string>('');

  // Sessions
  private sessions$ = this.isInstructor()
    ? this.auth.currentUser$.pipe(
        map(u => u?.userId ?? ''),
        switchMap(uid => this.api.getAllSessions().pipe(
          map(list => list.filter(s => s.instructorId?.toLowerCase() === uid.toLowerCase()))
        ))
      )
    : this.auth.currentUser$.pipe(
        map(u => u?.userId ?? ''),
        switchMap(uid => this.api.getAllSessions().pipe(
          map(list => list.filter(s => s.memberId?.toLowerCase() === uid.toLowerCase()))
        ))
      );

  allSessions = toSignal(this.sessions$, { initialValue: [] as PtSession[] });

  // Filter sessions for current week
  sessionsSig = computed(() => {
    const weekStart = this.weekStart();
    const weekEnd = new Date(weekStart.getTime());
    weekEnd.setDate(weekEnd.getDate() + 6);
    weekEnd.setHours(23, 59, 59, 999);
    
    return this.allSessions().filter(session => {
      const sessionDate = new Date(session.sessionTime);
      return sessionDate >= weekStart && sessionDate <= weekEnd;
    });
  });

  feedback = signal<string | null>(null);
  isLoading = signal(false);

getDayDates(): Date[] {
  const weekStart = this.weekStart();
  const days = this.days();
  
  return days.map(offset => {
    const d = new Date(weekStart.getTime());
    d.setDate(weekStart.getDate() + offset);
    d.setHours(0, 0, 0, 0);
    return d;
  });
}

getDayLabels(): string[] {
  const dates = this.getDayDates();
  return dates.map(d => 
    d.toLocaleDateString('en-US', { weekday: 'short', day: 'numeric' })
  );
}


  weekRange = computed(() => {
    const start = new Date(this.weekStart().getTime());
    const end = new Date(start.getTime());
    end.setDate(end.getDate() + 6);
    return {
      start: start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
      end: end.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    };
  });

slotISO = (dayIndex: number, hour: number) => {
  const dates = this.getDayDates();
  const base = dates[dayIndex];
  const d = new Date(base.getTime());
  d.setHours(hour, 0, 0, 0);
  return d.toISOString();
};

  isPastSlot = (iso: string) => {
    const now = new Date();
    return new Date(iso).getTime() < now.getTime();
  };

  slotState = (iso: string): SlotState => {
    const sessions = this.sessionsSig();
    const match = sessions.find(s => sameMinute(s.sessionTime, iso));
    if (!match) return 'available';
    if (this.isInstructor()) return 'booked';
    return match.memberId?.toLowerCase() === this.userId().toLowerCase() ? 'mine' : 'booked';
  };

  async book(iso: string) {
    if (!this.selectedInstructorId()) {
      this.feedback.set('Please select an instructor first.');
      return;
    }
    if (this.isPastSlot(iso)) {
      this.feedback.set('Cannot book past time slots.');
      return;
    }
    try {
      this.isLoading.set(true);
      await firstValueFrom(this.api.bookSession(this.selectedInstructorId(), iso));
      this.feedback.set('Session booked successfully.');
      this.refreshSessions();
    } catch {
      this.feedback.set('Failed to book session.');
    } finally {
      this.isLoading.set(false);
      setTimeout(() => this.feedback.set(null), 2500);
    }
  }

  async cancel(iso: string) {
    const sessions = this.sessionsSig();
    const s = sessions.find(x => sameMinute(x.sessionTime, iso));
    if (!s) return;
    try {
      this.isLoading.set(true);
      await firstValueFrom(this.api.cancelSession(s.id));
      this.feedback.set('Session cancelled.');
      this.refreshSessions();
    } catch {
      this.feedback.set('Failed to cancel session.');
    } finally {
      this.isLoading.set(false);
      setTimeout(() => this.feedback.set(null), 2500);
    }
  }

  prevWeek() {
    if (!this.canGoPrev()) return;
    const current = new Date(this.weekStart().getTime());
    const prev = addWeeks(current, -1);
    this.weekStart.set(prev);
  }

  nextWeek() {
    if (!this.canGoNext()) return;
    const current = new Date(this.weekStart().getTime());
    const next = addWeeks(current, 1);
    this.weekStart.set(next);
  }

  private refreshSessions() {    
    //placeholder 
  }
}

function startOfWeek(date: Date) {
  const d = new Date(date.getTime());
  const day = (d.getDay() + 6) % 7;
  d.setDate(d.getDate() - day);
  d.setHours(0, 0, 0, 0);
  return new Date(d.getTime());
}

function addWeeks(date: Date, weeks: number) {
  const d = new Date(date.getTime());
  d.setDate(d.getDate() + weeks * 7);
  d.setHours(0, 0, 0, 0);
  return new Date(d.getTime());
}

function sameMinute(aISO: string, bISO: string) {
  const a = new Date(aISO);
  const b = new Date(bISO);
  return a.getFullYear() === b.getFullYear()
    && a.getMonth() === b.getMonth()
    && a.getDate() === b.getDate()
    && a.getHours() === b.getHours()
    && a.getMinutes() === b.getMinutes();
}