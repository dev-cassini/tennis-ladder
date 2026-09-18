import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { LadderPlayer, LadderSetup } from '../../models/ladder.model';
import { LadderService } from '../../services/ladder.service';
import { shuffled } from '../../shared/shuffle';

type PlayerFormGroup = FormGroup<{
  displayName: FormControl<string>;
  email: FormControl<string>;
}>;

@Component({
  selector: 'app-ladder-setup',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './ladder-setup.component.html',
  styleUrl: './ladder-setup.component.scss'
})
export class LadderSetupComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ladderService = inject(LadderService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly ladderId = this.route.snapshot.paramMap.get('ladderId') ?? '';

  protected readonly setup = signal<LadderSetup | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly playerError = signal('');
  protected readonly orderError = signal('');
  protected readonly savingPlayers = signal(false);
  protected readonly savingOrder = signal(false);
  protected readonly launching = signal(false);
  protected readonly launchError = signal('');
  protected readonly rosterDirty = signal(false);
  protected readonly orderSaved = signal(true);
  protected readonly players = new FormArray<PlayerFormGroup>([]);
  protected readonly playerOrder = signal<LadderPlayer[]>([]);

  ngOnInit(): void {
    this.ladderService
      .getSetup(this.ladderId)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (setup) => this.applySetup(setup),
        error: (error: HttpErrorResponse) => {
          this.error.set(error.error?.title ?? 'We could not load this ladder.');
        }
      });
  }

  protected addPlayer(): void {
    this.players.push(this.createPlayerForm());
    this.markRosterDirty();
  }

  protected removePlayer(index: number): void {
    this.players.removeAt(index);
    this.markRosterDirty();
  }

  protected markRosterDirty(): void {
    this.rosterDirty.set(true);
    this.playerError.set('');
  }

  protected hasDuplicateEmail(index: number): boolean {
    const email = this.players.at(index).controls.email.value.trim().toLowerCase();

    if (!email) {
      return false;
    }

    return this.players.controls.filter(
      (control) => control.controls.email.value.trim().toLowerCase() === email
    ).length > 1;
  }

  protected savePlayers(): void {
    this.playerError.set('');
    this.players.markAllAsTouched();

    if (this.players.invalid || this.hasAnyDuplicateEmail() || this.savingPlayers()) {
      if (this.hasAnyDuplicateEmail()) {
        this.playerError.set('Each player email can appear only once.');
      }
      return;
    }

    const players = this.players.controls.map((control) => ({
      displayName: control.controls.displayName.value.trim(),
      email: control.controls.email.value.trim()
    }));

    this.savingPlayers.set(true);
    this.ladderService
      .replacePlayers(this.ladderId, players)
      .pipe(
        finalize(() => this.savingPlayers.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (setup) => {
          this.setup.set(setup);
          this.playerOrder.set([...setup.players]);
          this.rosterDirty.set(false);
          this.orderSaved.set(true);
        },
        error: (error: HttpErrorResponse) => {
          this.playerError.set(this.extractError(error, 'We could not save the players.'));
        }
      });
  }

  protected movePlayer(index: number, offset: -1 | 1): void {
    const targetIndex = index + offset;

    if (targetIndex < 0 || targetIndex >= this.playerOrder().length) {
      return;
    }

    const order = [...this.playerOrder()];
    [order[index], order[targetIndex]] = [order[targetIndex], order[index]];
    this.playerOrder.set(order);
    this.orderSaved.set(false);
    this.orderError.set('');
  }

  protected randomizeOrder(): void {
    this.playerOrder.set(shuffled(this.playerOrder()));
    this.orderSaved.set(false);
    this.orderError.set('');
  }

  protected saveOrder(): void {
    if (this.orderSaved() || this.savingOrder()) {
      return;
    }

    this.orderError.set('');
    this.savingOrder.set(true);
    this.ladderService
      .updatePlayerOrder(
        this.ladderId,
        this.playerOrder().map((player) => player.membershipId)
      )
      .pipe(
        finalize(() => this.savingOrder.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (setup) => {
          this.setup.set(setup);
          this.playerOrder.set([...setup.players]);
          this.orderSaved.set(true);
        },
        error: (error: HttpErrorResponse) => {
          this.orderError.set(this.extractError(error, 'We could not save the player order.'));
        }
      });
  }

  protected canLaunch(): boolean {
    return !this.rosterDirty() && this.orderSaved() && this.playerOrder().length >= 2;
  }

  protected launch(): void {
    if (!this.canLaunch() || this.launching()) {
      return;
    }

    this.launchError.set('');
    this.launching.set(true);
    this.ladderService
      .launchLadder(this.ladderId)
      .pipe(
        finalize(() => this.launching.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (ladder) => void this.router.navigate(['/app/ladders', ladder.id]),
        error: (error: HttpErrorResponse) => {
          this.launchError.set(this.extractError(error, 'We could not launch this ladder.'));
        }
      });
  }

  private applySetup(setup: LadderSetup): void {
    this.setup.set(setup);
    this.players.clear();

    if (setup.players.length === 0) {
      this.players.push(this.createPlayerForm());
      this.players.push(this.createPlayerForm());
      this.rosterDirty.set(true);
    } else {
      for (const player of setup.players) {
        this.players.push(this.createPlayerForm(player));
      }
      this.rosterDirty.set(false);
    }

    this.playerOrder.set([...setup.players]);
    this.orderSaved.set(true);
  }

  private createPlayerForm(player?: LadderPlayer): PlayerFormGroup {
    return new FormGroup({
      displayName: new FormControl(player?.displayName ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(200)]
      }),
      email: new FormControl(player?.email ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.email]
      })
    });
  }

  private hasAnyDuplicateEmail(): boolean {
    return this.players.controls.some((_, index) => this.hasDuplicateEmail(index));
  }

  private extractError(error: HttpErrorResponse, fallback: string): string {
    const validationErrors = error.error?.errors as Record<string, string[]> | undefined;
    const firstValidationError = validationErrors
      ? Object.values(validationErrors).flat()[0]
      : undefined;

    return firstValidationError ?? error.error?.title ?? fallback;
  }
}
