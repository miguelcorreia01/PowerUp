import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { startWith, switchMap, map, BehaviorSubject, firstValueFrom } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { GroupClassService, GroupClass, CreateGroupClassRequest } from '../../services/group-classes.service';
import { Nav } from '../../../../shared/components/nav/nav';

@Component({
  selector: 'app-group-classes',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, Nav],
  templateUrl: './group-classes.html',
  styleUrls: ['./group-classes.css']
})
export class GroupClasses {
  private auth = inject(AuthService);
  private api = inject(GroupClassService);

  // Week navigation
  weekStart = signal(this.getWeekStart(new Date()));
  weekStartSubject = new BehaviorSubject<Date>(this.weekStart());

  // Data signals
  groupClasses = toSignal(
    this.weekStartSubject.pipe(
      switchMap(weekStart => {
        const weekEnd = new Date(weekStart.getTime());
        weekEnd.setDate(weekEnd.getDate() + 6);
        weekEnd.setHours(23, 59, 59, 999);
        return this.api.getGroupClassesForWeek(weekStart, weekEnd);
      }),
      startWith([])
    ),
    { initialValue: [] }
  );

  // Role-based flags
  isInstructor = computed(() => this.auth.getRole() === 'Instructor');

  // Week navigation
  canGoPrev = computed(() => {
    const currentWeekStart = this.getWeekStart(new Date());
    return this.weekStart().getTime() > currentWeekStart.getTime();
  });

  canGoNext = computed(() => {
    const maxWeekStart = new Date();
    maxWeekStart.setDate(maxWeekStart.getDate() + 28);
    return this.weekStart().getTime() < this.getWeekStart(maxWeekStart).getTime();
  });

  weekRange = computed(() => {
    const start = this.weekStart();
    const end = new Date(start.getTime());
    end.setDate(end.getDate() + 6);
    return {
      start: start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
      end: end.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    };
  });

  // Days of the week
  days = signal([1, 2, 3, 4, 5, 6, 0]);
  dayLabels = computed(() => {
    const weekStart = this.weekStart();
    return this.days().map(offset => {
      const d = new Date(weekStart.getTime());
      d.setDate(weekStart.getDate() + offset);
      return d.toLocaleDateString('en-US', { weekday: 'short' });
    });
  });

  hours = signal([8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22]);

  // Create form
  showCreateForm = signal(false);
  createForm = signal<CreateGroupClassRequest>({
    instructorId: '',
    type: 'Yoga',
    name: '',
    description: '',
    startTime: '',
    maxCapacity: 10
  });

  // Feedback
  feedback = signal('');

  // Helper methods
  private getWeekStart(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day;
    const weekStart = new Date(d.setDate(diff));
    weekStart.setHours(0, 0, 0, 0);
    return weekStart;
  }

  prevWeek() {
    if (!this.canGoPrev()) return;
    const d = new Date(this.weekStart().getTime());
    d.setDate(d.getDate() - 7);
    this.weekStart.set(d);
    this.weekStartSubject.next(d);
  }

  nextWeek() {
    if (!this.canGoNext()) return;
    const d = new Date(this.weekStart().getTime());
    d.setDate(d.getDate() + 7);
    this.weekStart.set(d);
    this.weekStartSubject.next(d);
  }

  // Get classes for a specific day and hour
  getClassesForSlot(dayOffset: number, hour: number): GroupClass[] {
    const classes = this.groupClasses() || [];
    const targetDate = new Date(this.weekStart().getTime());
    targetDate.setDate(targetDate.getDate() + dayOffset);
    targetDate.setHours(hour, 0, 0, 0);

    return classes.filter(gc => {
      const classTime = new Date(gc.startTime);
      return classTime.getDate() === targetDate.getDate() &&
             classTime.getMonth() === targetDate.getMonth() &&
             classTime.getFullYear() === targetDate.getFullYear() &&
             classTime.getHours() === hour;
    });
  }

  // Check if user is enrolled in a class
isEnrolled(classId: string): boolean {
  const classes = this.groupClasses() || [];
  const groupClass = classes.find(gc => gc.id === classId);
  if (!groupClass) return false;

  const userId = this.auth.getUserId();
  return groupClass.members.some(member => member.userId?.toLowerCase() === userId?.toLowerCase());
}

// Enroll
async enroll(classId: string) {
  try {
    await firstValueFrom(this.api.enroll(classId));
    this.feedback.set('Enrolled successfully!');
    this.weekStartSubject.next(this.weekStart());
    setTimeout(() => this.feedback.set(''), 3000);
  } catch (error: any) {
    const msg = error?.error || 'Failed to enroll. Please try again.';
    this.feedback.set(msg);
    setTimeout(() => this.feedback.set(''), 3000);
  }
}

// Unenroll
async unenroll(classId: string) {
  try {
    await firstValueFrom(this.api.unenroll(classId));
    this.feedback.set('Unenrolled successfully!');
    this.weekStartSubject.next(this.weekStart());
    setTimeout(() => this.feedback.set(''), 3000);
  } catch (error: any) {
    const msg = error?.error || 'Failed to unenroll. Please try again.';
    this.feedback.set(msg);
    setTimeout(() => this.feedback.set(''), 3000);
  }
}

  // Create a new group class
  async createClass() {
    try {
      const form = this.createForm();
      if (!form.name || !form.startTime) {
        this.feedback.set('Please fill in all required fields.');
        return;
      }

      const userId = this.auth.getUserId();
      if (!userId) {
        this.feedback.set('You must be signed in to create a class.');
        setTimeout(() => this.feedback.set(''), 3000);
        return;
      }

      const request: CreateGroupClassRequest = {
        ...form,
        instructorId: userId
      };

      await this.api.createGroupClass(request).toPromise();
      this.feedback.set('Group class created successfully!');
      this.showCreateForm.set(false);
      this.createForm.set({
        instructorId: '',
        type: 'Yoga',
        name: '',
        description: '',
        startTime: '',
        maxCapacity: 10
      });
      
      
      this.weekStartSubject.next(this.weekStart());
    } catch (error) {
      this.feedback.set('Failed to create group class. Please try again.');
    }
  }

  // Cancel create form
  cancelCreate() {
    this.showCreateForm.set(false);
    this.createForm.set({
      instructorId: '',
      type: 'Yoga',
      name: '',
      description: '',
      startTime: '',
      maxCapacity: 10
    });
  }
}
