import { Component, ChangeDetectionStrategy, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, startWith } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { DashboardService } from '../../../dashboard/services/dashboard.service';
import {Nav} from '../../../../shared/components/nav/nav';

interface ScheduleItem {
  time: string;
  activity: string;
  type: 'group' | 'personal';
}

interface GroupClassItem {
  time: string;
  name: string;
  instructor: string;
  spots: number;
  maxSpots: number;
}

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink, Nav],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Dashboard {
  private auth = inject(AuthService);
  private api = inject(DashboardService);

  private currentUser$ = this.auth.currentUser$;
  private userSig = toSignal(this.currentUser$, { initialValue: null });

  userName = computed(() => {
    const user = this.userSig();
    return (user as any)?.name || (user?.userId ? 'Member' : 'User');
  });

  // Membership
  membership = toSignal(this.api.getMembership(), {
    initialValue: { type: '—', expiresIn: 0, progress: 0 }
  });

  // Group classes
  todayGroupClasses = toSignal(
    this.api.getAllGroupClasses().pipe(
      map(list => {
        const today = new Date();
        return list
          .filter(c => isSameDay(new Date(c.startTime), today))
          .sort((a, b) => +new Date(a.startTime) - +new Date(b.startTime))
          .map(c => ({
            time: formatHM(c.startTime),
            name: c.name,
            instructor: '',
            spots: c.currentEnrollment,
            maxSpots: c.maxCapacity
          } as GroupClassItem));
      }),
      startWith([] as GroupClassItem[])
    )
  );

  // Personal training
  private userId = computed(() => this.userSig()?.userId ?? '');
  todaySchedule = toSignal(
    this.api.getAllPtSessions().pipe(
      map(sessions => {
        const uid = this.userId();
        const today = new Date();

        const myTodayPT: ScheduleItem[] = sessions
          .filter(s => s.memberId?.toLowerCase() === uid.toLowerCase())
          .filter(s => isSameDay(new Date(s.sessionTime), today))
          .map(s => ({
            time: formatHM(s.sessionTime),
            activity: 'Personal Training',
            type: 'personal'
          }));

        return myTodayPT;
      }),
      startWith([] as ScheduleItem[])
    )
  );


  groupClasses = computed(() => {
    const classes = this.todayGroupClasses();
    return {
      enrolled: classes?.length ?? 0,
      nextClass: classes?.[0]?.time ?? '—'
    };
  });

  personalTraining = computed(() => {
    const schedule = this.todaySchedule();
    return {
      booked: schedule?.length ?? 0,
      nextSession: schedule?.[0]?.time ?? '—'
    };
  });

  // Role-based flags
  isInstructor = computed(() => (this.userSig()?.role ?? this.auth.getRole()) === 'Instructor');
  isMember = computed(() => !this.isInstructor());

  // Role-based labels
  classesCardTitle = computed(() => this.isInstructor() ? 'Your Group Classes' : 'Enrolled Group Classes');
  ptCardTitle = computed(() => this.isInstructor() ? 'Upcoming PT Sessions' : 'Booked Personal Training');

  labelType(item: ScheduleItem): string {
    return item.type === 'group' ? 'Group' : 'Personal';
  }
}

function isSameDay(a: Date, b: Date) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

function formatHM(date: string | Date) {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}